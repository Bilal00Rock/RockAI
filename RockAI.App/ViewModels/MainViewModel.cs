using Microsoft.Maui.Controls;
using RockAI.Application.Common.Enums;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
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
    public event Action? MessagesChanged;
    public string WelcomeMessage => $"Welcome, {_userSession.FullName}!";
    public Guid? UserId => _userSession.UserId;
    private readonly IAIService _aiService;

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
        }
    }

    public bool IsSendVisible => !IsGenerating;
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
        }
    }

    public ICommand NewConversationCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand StopGenerationCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainViewModel(
        IConversationService conversationService,
        IMessageService messageService,
        IUserSession userSession,
         IAIService aiService)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _userSession = userSession;
        _aiService = aiService;
        

        NewConversationCommand = new Command(async () => await CreateConversationAsync(), () => !IsBusy);
        SendMessageCommand = new Command(async () => await SendMessageAsync(), () => !IsBusy && !IsGenerating && SelectedConversation is not null && !string.IsNullOrWhiteSpace(MessageText));
        StopGenerationCommand = new Command(StopGeneration, () => IsGenerating);
        LogoutCommand = new Command(async () => await LogoutAsync(), () => !IsBusy);
    }
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

    private async Task LoadSelectedConversationAsync(
        ConversationViewModel? conversation,
        int selectionVersion)
    {
        if (conversation is null)
        {
            if (selectionVersion == _selectionVersion)
                Messages.Clear();

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
                Messages.Add(new MessageViewModel(message, RetryMessageAsync));

            MessagesChanged?.Invoke();

        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task SendMessageAsync()
    {
        if (SelectedConversation is null || string.IsNullOrWhiteSpace(MessageText) || IsBusy || IsGenerating)
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

            var result = await _messageService.SendMessageAsync(conversation.Id, MessageText.Trim(), cancellationToken);
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            Messages.Add(new MessageViewModel(result.Value.Message, RetryMessageAsync));

            if(!string.IsNullOrWhiteSpace(result.Value.NewTitle))
            {
                conversation.Title = result.Value.NewTitle;
            }

            MessagesChanged?.Invoke();
            MessageText = string.Empty;

            var assistantResult = await _messageService.CreateAssistantMessageAsync(
                conversation.Id,
                status: MessageStatus.Streaming,
                cancellationToken: cancellationToken);
            if (assistantResult.IsError)
            {
                SetError(assistantResult.Errors);
                return;
            }

            var assistantMessage = new MessageViewModel(assistantResult.Value, RetryMessageAsync);
            Messages.Add(assistantMessage);
            MessagesChanged?.Invoke();

            await GenerateAssistantAsync(conversation, assistantMessage, BuildRequest(), cancellationToken);
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
                BuildRequest(assistantMessage),
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
                    MessagesChanged?.Invoke();
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
                MessagesChanged?.Invoke();
        });

        if (result.IsError)
            SetError(result.Errors);
    }

    private AIChatRequest BuildRequest(MessageViewModel? excludedMessage = null) => new()
    {
        Task = AITask.Chat,
        Messages = Messages
            .Where(message => message != excludedMessage)
            .Select(message => new AIMessage
            {
                Role = message.Role switch
                {
                    "User" => AIMessageRole.User,
                    "Assistant" => AIMessageRole.Assistant,
                    "System" => AIMessageRole.System,
                    _ => AIMessageRole.User
                },
                Content = message.Content
            }).ToList()
    };

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

    private void SetError(IEnumerable<ErrorOr.Error> errors)
    {
        ErrorMessage = errors.FirstOrDefault().Description ?? "The operation failed.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}