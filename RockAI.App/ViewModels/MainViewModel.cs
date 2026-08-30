using Microsoft.Maui.Controls;
using RockAI.Application.Common.Enums;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Application.Attachments;
using RockAI.Domain.Attachments;
using RockAI.Domain.Conversations;
using RockAI.Domain.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace RockAI.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IConversationService _conversationService;
    private readonly IMessageService _messageService;
    private readonly IUserSession _userSession;
    private ConversationViewModel? _selectedConversation;
    private string _messageText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isGenerating;
    private CancellationTokenSource? _generationCts;
    private int _selectionVersion;
    private Task _selectedConversationLoadTask = Task.CompletedTask;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ConversationViewModel> Conversations { get; } = [];
    public ObservableCollection<MessageViewModel> Messages { get; } = [];
    public ObservableCollection<AttachmentChipViewModel> PendingAttachments { get; } = [];
    public event Action? MessagesChanged;
    public string WelcomeMessage => $"Welcome, {_userSession.FullName}!";
    public Guid? UserId => _userSession.UserId;
    private readonly IAIService _aiService;
    private readonly IFilePickerService _filePicker;
    private readonly IAttachmentService _attachmentService;
    private readonly IFileStorageService _fileStorage;

    public ConversationViewModel? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            if (_selectedConversation == value)
                return;

            _selectedConversation = value;
            OnPropertyChanged();

            StopGeneration();
            ((Command)SendMessageCommand).ChangeCanExecute();
            ((Command)DeleteConversationCommand).ChangeCanExecute();
            ((Command)EditConversationCommand).ChangeCanExecute();

            var selectionVersion = ++_selectionVersion;
            _selectedConversationLoadTask =
                LoadSelectedConversationAsync(value, selectionVersion);
        }
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        private set
        {
            if (_isGenerating == value)
                return;

            _isGenerating = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSendVisible));
            ((Command)SendMessageCommand).ChangeCanExecute();
            ((Command)StopGenerationCommand).ChangeCanExecute();
            ((Command)DeleteConversationCommand).ChangeCanExecute();
            ((Command)EditConversationCommand).ChangeCanExecute();
            UpdateMessageActionsEnabled();
        }
    }

    public bool IsSendVisible => !IsGenerating;

    /// <summary>True when the current conversation has no messages (empty-state UI).</summary>
    public bool HasNoMessages => Messages.Count == 0;

    public string MessageText
    {
        get => _messageText;
        set
        {
            if (_messageText == value)
                return;

            _messageText = value;
            OnPropertyChanged();
            ((Command)SendMessageCommand).ChangeCanExecute();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
            OnPropertyChanged();
            ((Command)NewConversationCommand).ChangeCanExecute();
            ((Command)SendMessageCommand).ChangeCanExecute();
            ((Command)DeleteConversationCommand).ChangeCanExecute();
            ((Command)EditConversationCommand).ChangeCanExecute();
            ((Command)LogoutCommand).ChangeCanExecute();
            UpdateMessageActionsEnabled();
        }
    }

    public ICommand NewConversationCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand StopGenerationCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand DeleteConversationCommand { get; }
    public ICommand EditConversationCommand { get; }

    public MainViewModel(
        IConversationService conversationService,
        IMessageService messageService,
        IUserSession userSession,
        IAIService aiService,
        IFilePickerService filePicker,
        IAttachmentService attachmentService,
        IFileStorageService fileStorage)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _userSession = userSession;
        _aiService = aiService;
        _filePicker = filePicker;
        _attachmentService = attachmentService;
        _fileStorage = fileStorage;

        NewConversationCommand = new Command(async () => await CreateConversationAsync(), () => !IsBusy);
        SendMessageCommand = new Command(
            async () => await SendMessageAsync(),
            () => !IsBusy && !IsGenerating && SelectedConversation is not null &&
                  (!string.IsNullOrWhiteSpace(MessageText) || PendingAttachments.Count > 0));
        StopGenerationCommand = new Command(StopGeneration, () => IsGenerating);
        LogoutCommand = new Command(async () => await LogoutAsync(), () => !IsBusy);
        DeleteConversationCommand = new Command(async () => await DeleteConversationAsync(), () => !IsBusy && !IsGenerating && SelectedConversation is not null);
        EditConversationCommand = new Command(async () => await EditConversationAsync(), () => !IsBusy && !IsGenerating && SelectedConversation is not null);
        AttachFilesCommand = new Command(async () => await AttachFilesAsync(), () => !IsBusy && !IsGenerating && SelectedConversation is not null);
    }

    public ICommand AttachFilesCommand { get; }
    private async Task LogoutAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await _userSession.ClearAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _conversationService.GetUserConversationsAsync(cancellationToken);
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            Conversations.Clear();
            foreach (var conversation in result.Value)
                Conversations.Add(new ConversationViewModel(conversation));

            if (Conversations.Count == 0)
            {
                SelectedConversation = null;
            }
            else if (SelectedConversation is null)
                SelectedConversation = Conversations[0];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateConversationAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _conversationService.CreateConversationAsync("New Conversation");
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }
            var conversationViewModel = new ConversationViewModel(result.Value);

            Conversations.Insert(0, conversationViewModel);
            SelectedConversation = conversationViewModel;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteConversationAsync()
    {
        if (SelectedConversation is null || IsBusy || IsGenerating)
            return;

        var conversationToDelete = SelectedConversation;

        bool confirmed;
        try
        {
            confirmed = await Shell.Current.DisplayAlert(
                "Delete conversation?",
                "This will permanently delete this conversation and its messages.",
                "Delete",
                "Cancel");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return;
        }

        if (!confirmed)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _conversationService.DeleteConversationAsync(conversationToDelete.Id);
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            var index = Conversations.IndexOf(conversationToDelete);
            if (index < 0)
                index = 0;

            Conversations.Remove(conversationToDelete);

            if (Conversations.Count == 0)
            {
                SelectedConversation = null;
            }
            else
            {
                var newIndex = Math.Min(index, Conversations.Count - 1);
                SelectedConversation = Conversations[newIndex];
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Unable to delete the conversation.";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(ex);
#endif
        }
        finally
        {
            IsBusy = false;
        }
    }


    private async Task LoadSelectedConversationAsync(
        ConversationViewModel? conversation,
        int selectionVersion)
    {
        if (conversation is null)
        {
            if (selectionVersion == _selectionVersion)
            {
                Messages.Clear();
                NotifyMessagesChanged();
            }

            return;
        }

        ErrorMessage = string.Empty;
        try
        {
            var result = await _messageService.GetMessagesAsync(conversation.Id);
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            if (selectionVersion != _selectionVersion ||
                !ReferenceEquals(_selectedConversation, conversation))
            {
                return;
            }

            Messages.Clear();
            foreach (var message in result.Value)
                Messages.Add(CreateMessageViewModel(message));// new MessageViewModel(message, RetryMessageAsync));

            NotifyMessagesChanged();

        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task SendMessageAsync()
    {
        if (SelectedConversation is null || IsBusy || IsGenerating)
            return;

        if (string.IsNullOrWhiteSpace(MessageText) && PendingAttachments.Count == 0)
            return;

        IsBusy = true;
        IsGenerating = true;
        _generationCts?.Dispose();
        _generationCts = new CancellationTokenSource();
        var cancellationToken = _generationCts.Token;
        ErrorMessage = string.Empty;
        var conversation = SelectedConversation;
        var generationCts = _generationCts;
        try
        {
            await _selectedConversationLoadTask;

            if (!ReferenceEquals(_selectedConversation, conversation) || cancellationToken.IsCancellationRequested)
                return;

            // Process pending attachments first
            IReadOnlyList<RockAI.Domain.Attachments.Attachment>? domainAttachments = null;
            if (PendingAttachments.Count > 0)
            {
                var picked = new List<PickedFile>();
                // Pending chips already hold processed domain attachments when we process on attach;
                // if still Selected, process now.
                var toProcess = PendingAttachments.Where(c => c.Attachment is null).ToList();
                // For this phase we process on attach (see AttachFilesAsync); use existing attachments.
                domainAttachments = PendingAttachments
                    .Where(c => c.Attachment is not null)
                    .Select(c => c.Attachment!)
                    .ToList();
            }

            var result = await _messageService.SendMessageAsync(
                conversation.Id,
                MessageText?.Trim() ?? string.Empty,
                domainAttachments,
                cancellationToken);
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            Messages.Add(CreateMessageViewModel(result.Value.Message));

            if (!string.IsNullOrWhiteSpace(result.Value.NewTitle))
            {
                conversation.Title = result.Value.NewTitle;
            }

            NotifyMessagesChanged();
            MessageText = string.Empty;
            PendingAttachments.Clear();
            ((Command)SendMessageCommand).ChangeCanExecute();

            var assistantResult = await _messageService.CreateAssistantMessageAsync(
                conversation.Id,
                status: MessageStatus.Streaming,
                cancellationToken: cancellationToken);
            if (assistantResult.IsError)
            {
                SetError(assistantResult.Errors);
                return;
            }

            var assistantMessage = CreateMessageViewModel(assistantResult.Value);
            Messages.Add(assistantMessage);
            NotifyMessagesChanged();

            await GenerateAssistantAsync(conversation, assistantMessage, await BuildRequestAsync(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = "Unable to send the message.";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(ex);
#endif
        }
        finally
        {
            CompleteGeneration(generationCts);
        }
    }


    private MessageViewModel CreateMessageViewModel(RockAI.Domain.Messages.Message message) =>
     new(message, RetryMessageAsync, EditMessageAsync, DeleteMessageAsync);

    private async Task RetryMessageAsync(MessageViewModel assistantMessage)
    {
        if (!assistantMessage.CanRetry || IsGenerating || SelectedConversation?.Id != assistantMessage.ConversationId)
            return;

        IsBusy = true;
        IsGenerating = true;
        _generationCts?.Dispose();
        _generationCts = new CancellationTokenSource();
        var cancellationToken = _generationCts.Token;
        var generationCts = _generationCts;
        var conversation = SelectedConversation!;
        ErrorMessage = string.Empty;

        try
        {
            var updateResult = await _messageService.UpdateMessageAsync(
                assistantMessage.Id,
                string.Empty,
                MessageRole.Assistant,
                MessageStatus.Streaming,
                cancellationToken);
            if (updateResult.IsError)
            {
                SetError(updateResult.Errors);
                return;
            }

            assistantMessage.ResetForRetry();
            await GenerateAssistantAsync(
                conversation,
                assistantMessage,
                await BuildRequestAsync(assistantMessage),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = "Unable to retry the response.";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(ex);
#endif
        }
        finally
        {
            CompleteGeneration(generationCts);
        }
    }

    private async Task GenerateAssistantAsync(
        ConversationViewModel conversation,
        MessageViewModel assistantMessage,
        AIChatRequest request,
        CancellationToken cancellationToken)
    {
        var pendingChunks = new StringBuilder();
        var lastUiUpdate = Stopwatch.GetTimestamp();
        var uiUpdateInterval = TimeSpan.FromMilliseconds(100);

        async Task FlushPendingChunksAsync()
        {
            if (pendingChunks.Length == 0)
                return;

            var content = pendingChunks.ToString();
            pendingChunks.Clear();
            lastUiUpdate = Stopwatch.GetTimestamp();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                assistantMessage.Append(content);
                if (ReferenceEquals(_selectedConversation, conversation))
                    NotifyMessagesChanged();
            });
        }

        try
        {
            await Task.Run(async () =>
            {
                await foreach (var chunk in _aiService
                    .GenerateStreamingAsync(request, cancellationToken)
                    .ConfigureAwait(false))
                {
                    pendingChunks.Append(chunk);
                    if (Stopwatch.GetElapsedTime(lastUiUpdate) >= uiUpdateInterval)
                        await FlushPendingChunksAsync();
                }
            }, cancellationToken);

            await FlushPendingChunksAsync();

            var status = cancellationToken.IsCancellationRequested
                ? MessageStatus.Cancelled
                : MessageStatus.Completed;
            await PersistAssistantStateAsync(assistantMessage, status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await FlushPendingChunksAsync();
            await PersistAssistantStateAsync(assistantMessage, MessageStatus.Cancelled);
        }
        catch
        {
            await FlushPendingChunksAsync();
            await PersistAssistantStateAsync(assistantMessage, MessageStatus.Failed);
            throw;
        }
    }

    private async Task PersistAssistantStateAsync(MessageViewModel assistantMessage, MessageStatus status)
    {
        var result = await _messageService.UpdateMessageAsync(
            assistantMessage.Id,
            assistantMessage.Content,
            MessageRole.Assistant,
            status,
            CancellationToken.None);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            assistantMessage.SetStatus(status);
            if (ReferenceEquals(_selectedConversation, SelectedConversation))
                NotifyMessagesChanged();
        });

        if (result.IsError)
            SetError(result.Errors);
    }

    private async Task<AIChatRequest> BuildRequestAsync(MessageViewModel? excludedMessage = null)
    {
        var messages = new List<AIMessage>();
        foreach (var message in Messages.Where(m => m != excludedMessage))
        {
            var content = message.Content ?? string.Empty;

            if (message.HasAttachments)
            {
                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrWhiteSpace(content))
                    sb.AppendLine(content).AppendLine();

                foreach (var chip in message.Attachments)
                {
                    var att = chip.Attachment;
                    if (att is null)
                        continue;

                    sb.AppendLine($"--- Attached document: {att.OriginalFileName} ({att.Extension.ToUpperInvariant()}) ---");
                    if (att.Status == AttachmentStatus.Ready)
                    {
                        try
                        {
                            var extractedPath = att.RelativePath + ".extracted.txt";
                            if (_fileStorage.Exists(extractedPath))
                            {
                                await using var stream = await _fileStorage.OpenReadAsync(extractedPath);
                                using var reader = new StreamReader(stream);
                                var extracted = await reader.ReadToEndAsync();
                                if (!string.IsNullOrWhiteSpace(extracted))
                                    sb.AppendLine(extracted.Trim());
                                else
                                    sb.AppendLine("[No extractable text]");
                            }
                            else
                            {
                                sb.AppendLine("[Document content unavailable]");
                            }
                        }
                        catch
                        {
                            sb.AppendLine("[Failed to load document content]");
                        }
                    }
                    else if (att.Status == AttachmentStatus.Failed)
                    {
                        sb.AppendLine($"[Could not process document: {att.ErrorMessage ?? "unknown error"}]");
                    }
                    else
                    {
                        sb.AppendLine("[Document not ready]");
                    }
                    sb.AppendLine("--- End of document ---").AppendLine();
                }

                content = sb.ToString().Trim();
            }

            messages.Add(new AIMessage
            {
                Role = message.Role switch
                {
                    "User" => AIMessageRole.User,
                    "Assistant" => AIMessageRole.Assistant,
                    "System" => AIMessageRole.System,
                    _ => AIMessageRole.User
                },
                Content = content
            });
        }

        return new AIChatRequest
        {
            Task = AITask.Chat,
            Messages = messages
        };
    }


    private void StopGeneration() => _generationCts?.Cancel();

    private void CompleteGeneration(CancellationTokenSource generationCts)
    {
        if (!ReferenceEquals(_generationCts, generationCts))
            return;

        _generationCts = null;
        generationCts.Dispose();
        IsGenerating = false;
        IsBusy = false;
    }
    private void UpdateMessageActionsEnabled()
    {
        var enabled = !IsGenerating && !IsBusy;
        foreach (var message in Messages)
            message.SetActionsEnabled(enabled);
    }

    private async Task EditMessageAsync(MessageViewModel message)
    {
        if (IsBusy || IsGenerating)
            return;

        if (SelectedConversation?.Id != message.ConversationId)
            return;

        if (message.MessageRole != MessageRole.User)
            return;

        string? newContent;
        try
        {
            newContent = await Shell.Current.DisplayPromptAsync(
                "Edit message",
                "Update the message content. Later messages will be removed and a new reply generated.",
                accept: "Save",
                cancel: "Cancel",
                placeholder: "Message",
                maxLength: 4000,
                keyboard: Keyboard.Text,
                initialValue: message.Content);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return;
        }

        if (newContent is null)
            return;

        if (string.IsNullOrWhiteSpace(newContent))
        {
            ErrorMessage = "Message content cannot be empty.";
            return;
        }

        var conversation = SelectedConversation;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _messageService.EditMessageContentAsync(message.Id, newContent.Trim());
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            // Persist succeeded: update UI content and drop subsequent messages.
            message.SetContent(result.Value.Content);

            var index = Messages.IndexOf(message);
            if (index >= 0)
            {
                for (var i = Messages.Count - 1; i > index; i--)
                    Messages.RemoveAt(i);
            }

            NotifyMessagesChanged();

            // Reuse existing send/generation pipeline for a new assistant reply.
            IsGenerating = true;
            _generationCts?.Dispose();
            _generationCts = new CancellationTokenSource();
            var cancellationToken = _generationCts.Token;
            var generationCts = _generationCts;

            try
            {
                var assistantResult = await _messageService.CreateAssistantMessageAsync(
                    conversation.Id,
                    status: MessageStatus.Streaming,
                    cancellationToken: cancellationToken);
                if (assistantResult.IsError)
                {
                    SetError(assistantResult.Errors);
                    return;
                }

                var assistantMessage = CreateMessageViewModel(assistantResult.Value);
                Messages.Add(assistantMessage);
                NotifyMessagesChanged();

                await GenerateAssistantAsync(conversation, assistantMessage, await BuildRequestAsync(), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                CompleteGeneration(generationCts);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Unable to edit the message.";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(ex);
#endif
            IsBusy = false;
            IsGenerating = false;
        }
    }

    private async Task DeleteMessageAsync(MessageViewModel message)
    {
        if (IsBusy || IsGenerating)
            return;

        if (SelectedConversation?.Id != message.ConversationId)
            return;

        var isUser = message.MessageRole == MessageRole.User;
        bool confirmed;
        try
        {
            confirmed = await Shell.Current.DisplayAlert(
                "Delete message?",
                isUser
                    ? "This will permanently delete this message and its assistant reply (if any)."
                    : "This will permanently delete this message.",
                "Delete",
                "Cancel");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return;
        }

        if (!confirmed)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var index = Messages.IndexOf(message);
            var result = await _messageService.DeleteMessageAsync(message.Id);
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            // Match service behavior: user delete also removes following assistant in UI.
            if (isUser && index >= 0 && index + 1 < Messages.Count &&
                Messages[index + 1].MessageRole == MessageRole.Assistant)
            {
                Messages.RemoveAt(index + 1);
            }

            Messages.Remove(message);
            NotifyMessagesChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Unable to delete the message.";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(ex);
#endif
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EditConversationAsync()
    {
        if (SelectedConversation is null || IsBusy || IsGenerating)
            return;

        var conversation = SelectedConversation;

        string? newTitle;
        try
        {
            newTitle = await Shell.Current.DisplayPromptAsync(
                "Edit conversation",
                "Enter a new title:",
                accept: "Save",
                cancel: "Cancel",
                placeholder: "Title",
                maxLength: 200,
                keyboard: Keyboard.Text,
                initialValue: conversation.Title);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return;
        }

        if (newTitle is null)
            return;

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            ErrorMessage = "Conversation title cannot be empty.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var existing = await _conversationService.GetConversationAsync(conversation.Id);
            if (existing.IsError)
            {
                SetError(existing.Errors);
                return;
            }

            var result = await _conversationService.UpdateConversationAsync(
                conversation.Id,
                newTitle.Trim(),
                existing.Value.ConversationType,
                existing.Value.IsCompleted);

            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            conversation.Title = result.Value.Title;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Unable to rename the conversation.";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(ex);
#endif
        }
        finally
        {
            IsBusy = false;
        }
    }


    private async Task AttachFilesAsync()
    {
        if (SelectedConversation is null || IsBusy || IsGenerating)
            return;

        try
        {
            var files = await _filePicker.PickFilesAsync(cancellationToken: CancellationToken.None);
            if (files.Count == 0)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            var chips = new List<AttachmentChipViewModel>();
            foreach (var f in files)
            {
                var ext = System.IO.Path.GetExtension(f.FileName).TrimStart('.').ToLowerInvariant();
                var chip = new AttachmentChipViewModel(f.FileName, ext, f.SizeBytes);
                chip.ApplyStatus(AttachmentStatus.Processing);
                chip.RemoveCommand = new Command(() => RemovePendingAttachment(chip));
                chips.Add(chip);
                PendingAttachments.Add(chip);
            }
            ((Command)SendMessageCommand).ChangeCanExecute();

            var result = await _attachmentService.CreateAndProcessAsync(
                SelectedConversation.Id,
                messageId: Guid.Empty,
                files,
                createdBy: _userSession.UserId);

            if (result.IsError)
            {
                SetError(result.Errors);
                foreach (var c in chips)
                    c.ApplyStatus(AttachmentStatus.Failed, "Processing failed");
                return;
            }

            for (var i = 0; i < chips.Count && i < result.Value.Count; i++)
            {
                var att = result.Value[i];
                chips[i].Attachment = att;
                chips[i].ApplyStatus(att.Status, att.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Unable to attach files.";
#if DEBUG
            System.Diagnostics.Debug.WriteLine(ex);
#endif
        }
        finally
        {
            IsBusy = false;
            ((Command)SendMessageCommand).ChangeCanExecute();
        }
    }

    private void RemovePendingAttachment(AttachmentChipViewModel chip)
    {
        if (chip is null) return;
        PendingAttachments.Remove(chip);
        ((Command)SendMessageCommand).ChangeCanExecute();
    }

    private void SetError(IEnumerable<ErrorOr.Error> errors)
    {
        ErrorMessage = errors.FirstOrDefault().Description ?? "The operation failed.";
    }

    private void NotifyMessagesChanged()
    {
        OnPropertyChanged(nameof(HasNoMessages));
        MessagesChanged?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}