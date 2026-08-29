using RockAI.Domain.Messages;

namespace RockAI.Common.Tests.Builders;

public sealed class MessageBuilder
{
    private MessageRole _role = MessageRole.User;
    private string _content = "Test message";
    private Guid _conversationId = Guid.NewGuid();
    private Guid? _id;
    private DateTime? _createdAt;
    private MessageStatus _status = MessageStatus.Pending;

    public MessageBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public MessageBuilder ForConversation(Guid conversationId)
    {
        _conversationId = conversationId;
        return this;
    }

    public MessageBuilder WithRole(MessageRole role)
    {
        _role = role;
        return this;
    }

    public MessageBuilder WithStatus(MessageStatus status)
    {
        _status = status;
        return this;
    }

    public MessageBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public MessageBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public Message Build() => new(
        _role,
        _content,
        _conversationId,
        _id,
        _createdAt,
        _status);
}
