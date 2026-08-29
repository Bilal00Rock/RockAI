using System.ComponentModel;
using System.Runtime.CompilerServices;
using RockAI.Domain.Conversations;

namespace RockAI.App.ViewModels;

public sealed class ConversationViewModel : INotifyPropertyChanged
{
    private string _title;

    public Guid Id { get; }

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
                return;

            _title = value;
            OnPropertyChanged();
        }
    }

    public ConversationViewModel(Conversation conversation)
    {
        Id = conversation.Id;
        _title = conversation.Title;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}