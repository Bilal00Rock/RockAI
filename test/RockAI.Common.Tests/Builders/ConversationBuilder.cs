using RockAI.Domain.Conversations;

namespace RockAI.Common.Tests.Builders;

public sealed class ConversationBuilder
{
    private ConversationType _conversationType = ConversationType.General;
    private string _title = "Test conversation";
    private Guid _userId = Guid.NewGuid();
    private Guid? _id;
    private DateTime? _createdAt;
    private bool _isCompleted;
    private Guid? _createdBy;

    public ConversationBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public ConversationBuilder ForUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public ConversationBuilder WithType(ConversationType conversationType)
    {
        _conversationType = conversationType;
        return this;
    }

    public ConversationBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ConversationBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public ConversationBuilder CreatedBy(Guid createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public ConversationBuilder Completed()
    {
        _isCompleted = true;
        return this;
    }

    public Conversation Build() => new(
        _conversationType,
        _title,
        _userId,
        _id,
        _createdAt,
        _isCompleted,
        _createdBy);
}
