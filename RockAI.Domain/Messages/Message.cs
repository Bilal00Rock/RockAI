using ErrorOr;
using RockAI.Domain.Attachments;
using RockAI.Domain.Common.Interfaces;

namespace RockAI.Domain.Messages;

public class Message : Entity
{
    public string Content { get; private set; }
    public MessageRole MessageRole { get; private set; }
    public Guid ConversationId { get; }
    public MessageStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<Attachment> _attachments = [];
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();

    public Message(
        MessageRole messageRole,
        string content,
        Guid conversationId,
        Guid? id = null,
        DateTime? createdAt = null,
        MessageStatus? status = null,
        Guid? createdBy = null)
            : base(id ?? Guid.NewGuid())
    {
        // Allow empty content when role is Assistant or when attachments will be added.
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
        Content = content ?? string.Empty;
        ConversationId = conversationId;
        Status = status ?? MessageStatus.Pending;
        SetCreatedAudit(createdBy, createdAt);
    }

    private Message()
    {
        Content = string.Empty;
        MessageRole = null!;
    }

    public ErrorOr<Success> UpdateMessage(string content, MessageRole messageRole, MessageStatus status, Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(content) && messageRole != MessageRole.Assistant && _attachments.Count == 0)
        {
            return MessageErrors.InvalidContent;
        }

        Content = content ?? string.Empty;
        MessageRole = messageRole;
        Status = status;
        CompletedAt = status == MessageStatus.Completed ||
            status == MessageStatus.Failed ||
            status == MessageStatus.Cancelled
            ? DateTime.UtcNow
            : null;

        SetUpdatedAudit(updatedBy);
        return Result.Success;
    }

    public ErrorOr<Success> AddAttachment(Attachment attachment)
    {
        if (attachment is null)
            return MessageErrors.InvalidContent; // reuse or add specific later

        if (attachment.MessageId != Id && attachment.MessageId != Guid.Empty)
            return Error.Validation("Message.AttachmentMismatch", "Attachment does not belong to this message.");

        _attachments.Add(attachment);
        SetUpdatedAudit();
        return Result.Success;
    }

    internal void SetAttachments(IEnumerable<Attachment> attachments)
    {
        _attachments.Clear();
        _attachments.AddRange(attachments);
    }
}
