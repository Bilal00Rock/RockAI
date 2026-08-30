using ErrorOr;
using RockAI.Domain.Common.Interfaces;

namespace RockAI.Domain.Conversations;

public class Conversation : Entity
{
    public string Title { get; set; }
    public ConversationType ConversationType { get; private set; }
    public Guid UserId { get; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public Conversation(
        ConversationType conversationType,
        string title,
        Guid userId,
        Guid? id = null,
        DateTime? createdAt = null,
        bool isCompleted = false,
        Guid? createdBy = null)
            : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Conversation title cannot be empty.",
                nameof(title));
        }
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }
        ConversationType = conversationType;
        Title = title;
        UserId = userId;
        IsCompleted = isCompleted;
        SetCreatedAudit(createdBy ?? userId, createdAt);
        if (isCompleted)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    private Conversation()
    {
        Title = string.Empty;
        ConversationType = null!;
    }

    public ErrorOr<Success> UpdateConversation(string title, ConversationType conversationType, bool isCompleted, Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ConversationErrors.InvalidTitle;
        }
        Title = title;
        ConversationType = conversationType;

        if (isCompleted && !IsCompleted)
        {
            MarkAsCompleted();
        }
        else if (!isCompleted && IsCompleted)
        {
            MarkAsIncomplete();
        }

        SetUpdatedAudit(updatedBy);
        return Result.Success;
    }

    public void MarkAsCompleted()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        SetUpdatedAudit();
    }

    public void MarkAsIncomplete()
    {
        IsCompleted = false;
        CompletedAt = null;
        SetUpdatedAudit();
    }
}
