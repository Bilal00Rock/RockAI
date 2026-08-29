using RockAI.Application.Common.Enums;
using RockAI.Domain.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace RockAI.App.ViewModels;

public sealed class MessageViewModel : INotifyPropertyChanged
{
    public string Role { get; }

    private string _content;

    public string Content
    {
        get => _content;
        private set
        {
            if (_content == value)
                return;

            _content = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(Content)));
        }
    }

    public MessageViewModel(Message message)
    {
        Role = message.MessageRole.Name;
        Content = message.Content;
    }

    public MessageViewModel(AIMessageRole role)
    {
        Role = role.Name;
        Content = string.Empty;
    }

    public void Append(string chunk)
    {
        Content += chunk;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
