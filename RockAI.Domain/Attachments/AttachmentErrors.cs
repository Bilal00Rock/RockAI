using ErrorOr;

namespace RockAI.Domain.Attachments;

public static class AttachmentErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Attachment.NotFound",
        description: "Attachment was not found.");

    public static readonly Error UnsupportedFileType = Error.Validation(
        code: "Attachment.UnsupportedFileType",
        description: "The selected file type is not supported.");

    public static readonly Error FileTooLarge = Error.Validation(
        code: "Attachment.FileTooLarge",
        description: "The file exceeds the maximum allowed size.");

    public static readonly Error EmptyDocument = Error.Validation(
        code: "Attachment.EmptyDocument",
        description: "The document contains no extractable content.");

    public static readonly Error ProcessingFailed = Error.Failure(
        code: "Attachment.ProcessingFailed",
        description: "Document processing failed.");

    public static readonly Error InvalidStatusTransition = Error.Validation(
        code: "Attachment.InvalidStatusTransition",
        description: "The attachment cannot transition to the requested status.");

    public static readonly Error FileNotFound = Error.NotFound(
        code: "Attachment.FileNotFound",
        description: "The attachment file was not found on disk.");
}
