using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Conversations;
using RockAI.Domain.Messages;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RockAI.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IConversationService _conversationService;
    private readonly IMessageService _messageService;
    private readonly IUserSession _userSession;
    private Conversation? _selectedConversation;
    private string _messageText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private int _selectionVersion;
    private Task _selectedConversationLoadTask = Task.CompletedTask;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Conversation> Conversations { get; } = [];
    public ObservableCollection<Message> Messages { get; } = [];

    public string WelcomeMessage => $"Welcome, {_userSession.FullName}!";
    public Guid? UserId => _userSession.UserId;

    public Conversation? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            if (_selectedConversation == value)
                return;

            _selectedConversation = value;
            OnPropertyChanged();
            var selectionVersion = ++_selectionVersion;
            _selectedConversationLoadTask = LoadSelectedConversationAsync(value, selectionVersion);
        }
    }

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
    public ICommand LogoutCommand { get; }

    public MainViewModel(
        IConversationService conversationService,
        IMessageService messageService,
        IUserSession userSession)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _userSession = userSession;

        NewConversationCommand = new Command(async () => await CreateConversationAsync(), () => !IsBusy);
        SendMessageCommand = new Command(async () => await SendMessageAsync(), () => !IsBusy && SelectedConversation is not null && !string.IsNullOrWhiteSpace(MessageText));
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
                Conversations.Add(conversation);

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

            Conversations.Insert(0, result.Value);
            SelectedConversation = result.Value;
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
        Conversation? conversation,
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
                Messages.Add(message);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task SendMessageAsync()
    {
        if (SelectedConversation is null || string.IsNullOrWhiteSpace(MessageText) || IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var conversation = SelectedConversation;
            if (conversation is null)
                return;

            await _selectedConversationLoadTask;

            if (!ReferenceEquals(_selectedConversation, conversation))
                return;

            var result = await _messageService.SendMessageAsync(conversation.Id, MessageText.Trim());
            if (result.IsError)
            {
                SetError(result.Errors);
                return;
            }

            Messages.Add(result.Value);
            MessageText = string.Empty;

            var conversationResult = await _conversationService.GetConversationAsync(conversation.Id);
            if (!conversationResult.IsError)
            {
                // Replacing the selected item can reset CollectionView.SelectedItem,
                // which starts another load and clears the message just added above.
                // var index = Conversations.IndexOf(conversation);
                // if (index >= 0)
                //     Conversations[index] = conversationResult.Value;

                _selectedConversation = conversationResult.Value;
                OnPropertyChanged(nameof(SelectedConversation));
            }
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

    private void SetError(IEnumerable<ErrorOr.Error> errors)
    {
        ErrorMessage = errors.FirstOrDefault().Description ?? "The operation failed.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}