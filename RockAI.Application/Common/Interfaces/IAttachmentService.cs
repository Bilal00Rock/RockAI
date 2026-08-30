using ErrorOr;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Application.Common.Interfaces;

public interface IAttachmentService
{
    /// <summary>
    /// Stores picked files under the conversation, creates Attachment entities (Status=Stored),
    /// then processes each one (Status=Processing → Ready/Failed).
    /// MessageId may be Guid.Empty until the message is persisted; call AssignToMessage later.
    /// </summary>
    Task<ErrorOr<IReadOnlyList<Attachment>>> CreateAndProcessAsync(
        Guid conversationId,
        Guid messageId,
        IReadOnlyList<PickedFile> files,
        Guid? createdBy = null,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<DocumentProcessingResult>> ReprocessAsync(
        Attachment attachment,
        CancellationToken cancellationToken = default);
}
