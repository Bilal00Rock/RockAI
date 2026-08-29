using System.Runtime.CompilerServices;
using System;
using ErrorOr;
using RockAI.Domain.Common.Interfaces;

namespace RockAI.Domain.Messages;

public class Message : Entity
{
    public string Content { get; private set; }
    public MessageRole MessageRole { get; private set; }
    public Guid ConversationId { get; }
    public MessageStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public Message(
        MessageRole messageRole,
        string content,
        Guid conversationId,
        Guid? id = null,
        DateTime? createdAt = null,  
        MessageStatus? status = null)
            : base(id ?? Guid.NewGuid() )
    {
        if (string.IsNullOrWhiteSpace(content) && messageRole != MessageRole.Assistant)
            throw new ArgumentException(
                "Message content cannot be empty.",
                nameof(content));

        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Conversation ID cannot be empty.",
                nameof(conversationId));
        }

        MessageRole = messageRole;
        Content = content;
        ConversationId = conversationId;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        Status = status ?? MessageStatus.Pending;
    }

    private Message()
    {
        Content = string.Empty;
        MessageRole = null!; 
    }
    
    public ErrorOr<Success> UpdateMessage(string content, MessageRole messageRole, MessageStatus status)
    {
        if (string.IsNullOrWhiteSpace(content) && messageRole != MessageRole.Assistant)
        {
            return MessageErrors.InvalidContent;
        }
        Content = content;
        MessageRole = messageRole;
        Status = status;
        CompletedAt = status == MessageStatus.Completed ||
            status == MessageStatus.Failed ||
            status == MessageStatus.Cancelled
            ? DateTime.UtcNow
            : null;

        return Result.Success;
    }
}