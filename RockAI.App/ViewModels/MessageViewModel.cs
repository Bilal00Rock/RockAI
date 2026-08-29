using Microsoft.Maui.Controls.PlatformConfiguration;
using RockAI.Application.Common.Enums;
using RockAI.Domain.Messages;
using System.ComponentModel;
using System.Windows.Input;

namespace RockAI.App.ViewModels;

public sealed class MessageViewModel : INotifyPropertyChanged
{
    private readonly Func<MessageViewModel, Task>? _retryAction;
    private string _content;
    private MessageStatus _status;

    public Guid Id { get; }
    public Guid ConversationId { get; }
    public string Role { get; }

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
        }
    }

    public string StatusText => Status == MessageStatus.Cancelled
        ? "[Stopped]"
        : Status == MessageStatus.Failed
            ? "[Failed]"
            : string.Empty;

    public bool CanRetry => _retryAction is not null &&
        (Status == MessageStatus.Failed || Status == MessageStatus.Cancelled);

    public ICommand RetryCommand { get; }

    public MessageViewModel(Message message, Func<MessageViewModel, Task>? retryAction = null)
    {
        Id = message.Id;
        ConversationId = message.ConversationId;
        Role = message.MessageRole.Name;
        _content = message.Content;
        _status = message.Status;
        _retryAction = retryAction;
        RetryCommand = new Command(async () =>
        {
            if (CanRetry)
                await _retryAction!(this);
        }, () => CanRetry);
    }

    public void Append(string chunk) => Content += chunk;

    public void ResetForRetry()
    {
        Content = string.Empty;
        SetStatus(MessageStatus.Streaming);
    }

    public void SetStatus(MessageStatus status)
    {
        Status = status;
        ((Command)RetryCommand).ChangeCanExecute();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
