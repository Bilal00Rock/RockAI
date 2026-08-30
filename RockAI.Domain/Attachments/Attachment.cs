using ErrorOr;
using RockAI.Domain.Common.Interfaces;

namespace RockAI.Domain.Attachments;

public class Attachment : Entity
{
    public Guid MessageId { get; private set; }
    public string FileName { get; private set; }
    public string OriginalFileName { get; private set; }
    public string Extension { get; private set; }
    public string MimeType { get; private set; }
    public long SizeBytes { get; private set; }
    public string RelativePath { get; private set; }
    public AttachmentStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public Attachment(
        Guid messageId,
        string originalFileName,
        string fileName,
        string extension,
        string mimeType,
        long sizeBytes,
        string relativePath,
        Guid? id = null,
        DateTime? createdAt = null,
        Guid? createdBy = null,
        AttachmentStatus? status = null)
        : base(id ?? Guid.NewGuid())
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name cannot be empty.", nameof(originalFileName));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be empty.", nameof(extension));
        if (sizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path cannot be empty.", nameof(relativePath));

        MessageId = messageId;
        OriginalFileName = originalFileName.Trim();
        FileName = fileName.Trim();
        Extension = extension.Trim().TrimStart('.').ToLowerInvariant();
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim();
        SizeBytes = sizeBytes;
        RelativePath = relativePath.Trim();
        Status = status ?? AttachmentStatus.Stored;
        SetCreatedAudit(createdBy, createdAt);
    }

    private Attachment()
    {
        FileName = string.Empty;
        OriginalFileName = string.Empty;
        Extension = string.Empty;
        MimeType = string.Empty;
        RelativePath = string.Empty;
        Status = null!;
    }

    public ErrorOr<Success> MarkProcessing(Guid? updatedBy = null)
    {
        if (Status == AttachmentStatus.Ready || Status == AttachmentStatus.Failed)
            return AttachmentErrors.InvalidStatusTransition;

        Status = AttachmentStatus.Processing;
        ErrorMessage = null;
        SetUpdatedAudit(updatedBy);
        return Result.Success;
    }

    public ErrorOr<Success> MarkReady(Guid? updatedBy = null)
    {
        Status = AttachmentStatus.Ready;
        ErrorMessage = null;
        ProcessedAt = DateTime.UtcNow;
        SetUpdatedAudit(updatedBy);
        return Result.Success;
    }

    public ErrorOr<Success> MarkFailed(string errorMessage, Guid? updatedBy = null)
    {
        Status = AttachmentStatus.Failed;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
            ? "Document processing failed."
            : errorMessage.Trim();
        ProcessedAt = DateTime.UtcNow;
        SetUpdatedAudit(updatedBy);
        return Result.Success;
    }

    public ErrorOr<Success> AssignToMessage(Guid messageId, Guid? updatedBy = null)
    {
        if (messageId == Guid.Empty)
            return Error.Validation("Attachment.InvalidMessageId", "Message ID cannot be empty.");

        MessageId = messageId;
        SetUpdatedAudit(updatedBy);
        return Result.Success;
    }
}
