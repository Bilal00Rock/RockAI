using Microsoft.Maui.Controls.PlatformConfiguration;
using RockAI.Application.Common.Enums;
using RockAI.Domain.Messages;
using System.ComponentModel;
using System.Windows.Input;

namespace RockAI.App.ViewModels;

public sealed class MessageViewModel : INotifyPropertyChanged
{
    private readonly Func<MessageViewModel, Task>? _retryAction;
    private readonly Func<MessageViewModel, Task>? _editAction;
    private readonly Func<MessageViewModel, Task>? _deleteAction;
    private string _content;
    private MessageStatus _status;
    private bool _actionsEnabled = true;
    public Guid Id { get; }
    public Guid ConversationId { get; }
    public string Role { get; }
    public MessageRole MessageRole { get; }
    public string Content
    {
        get => _content;
        private set
        {
            if (_content == value)
                return;

            _content = value;
            OnPropertyChanged();
        }
    }

    public MessageStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value)
                return;

            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(CanRetry));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            RaiseCommandCanExecuteChanged();
        }
    }

    public string StatusText => Status == MessageStatus.Cancelled
        ? "[Stopped]"
        : Status == MessageStatus.Failed
            ? "[Failed]"
            : string.Empty;

    public bool CanRetry => _retryAction is not null &&
        (Status == MessageStatus.Failed || Status == MessageStatus.Cancelled);
    /// <summary>User messages only, not while streaming, and actions not disabled globally.</summary>
    public bool CanEdit => _editAction is not null &&
        _actionsEnabled &&
        Status != MessageStatus.Streaming;

    public bool CanDelete => _deleteAction is not null &&
        _actionsEnabled &&
        Status != MessageStatus.Streaming;

    public ICommand RetryCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public MessageViewModel(Message message, Func<MessageViewModel, Task>? retryAction = null, Func<MessageViewModel, Task>? editAction = null, Func<MessageViewModel, Task>? deleteAction = null)
    {
        Id = message.Id;
        ConversationId = message.ConversationId;
        Role = message.MessageRole.Name;
        _content = message.Content;
        _status = message.Status;
        _retryAction = retryAction;
        _editAction = editAction;
        _deleteAction = deleteAction;
        RetryCommand = new Command(async () =>
        {
            if (CanRetry)
                await _retryAction!(this);
        }, () => CanRetry);

        EditCommand = new Command(async () =>
        {
            if (CanEdit)
                await _editAction!(this);
        }, () => CanEdit);

        DeleteCommand = new Command(async () =>
        {
            if (CanDelete)
                await _deleteAction!(this);
        }, () => CanDelete);
    }

    public void Append(string chunk) => Content += chunk;

    public void ResetForRetry()
    {
        Content = string.Empty;
        SetStatus(MessageStatus.Streaming);
    }
    public void SetContent(string content) => Content = content;
    public void SetStatus(MessageStatus status)
    {
        Status = status;
        ((Command)RetryCommand).ChangeCanExecute();
    }
    public void SetActionsEnabled(bool enabled)
    {
        if (_actionsEnabled == enabled)
            return;

        _actionsEnabled = enabled;
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanDelete));
        RaiseCommandCanExecuteChanged();
    }

    private void RaiseCommandCanExecuteChanged()
    {
        ((Command)RetryCommand).ChangeCanExecute();
        ((Command)EditCommand).ChangeCanExecute();
        ((Command)DeleteCommand).ChangeCanExecute();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
