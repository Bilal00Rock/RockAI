using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Application.Documents;
using RockAI.Domain.Attachments;

namespace RockAI.Application.Attachments;

public sealed class AttachmentService : IAttachmentService
{
    private readonly IFileStorageService _fileStorage;
    private readonly IDocumentProcessor _documentProcessor;

    public AttachmentService(
        IFileStorageService fileStorage,
        IDocumentProcessor documentProcessor)
    {
        _fileStorage = fileStorage;
        _documentProcessor = documentProcessor;
    }

    public async Task<ErrorOr<IReadOnlyList<Attachment>>> CreateAndProcessAsync(
        Guid conversationId,
        Guid messageId,
        IReadOnlyList<PickedFile> files,
        Guid? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        if (files is null || files.Count == 0)
            return Array.Empty<Attachment>();

        var results = new List<Attachment>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var originalName = string.IsNullOrWhiteSpace(file.FileName) ? "file" : file.FileName;
            var safeName = _fileStorage.SanitizeFileName(originalName);
            var extension = Path.GetExtension(safeName).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                extension = "bin";

            if (!_documentProcessor.IsSupported(extension, file.ContentType))
            {
                // Still store unsupported files as Failed so UI can show them
                var failedId = Guid.NewGuid();
                var failedRelative = BuildRelativePath(conversationId, failedId, safeName);
                var failedAttachment = new Attachment(
                    messageId: messageId == Guid.Empty ? failedId : messageId,
                    originalFileName: originalName,
                    fileName: safeName,
                    extension: extension,
                    mimeType: file.ContentType ?? "application/octet-stream",
                    sizeBytes: file.SizeBytes,
                    relativePath: failedRelative,
                    id: failedId,
                    createdBy: createdBy,
                    status: AttachmentStatus.Failed);
                failedAttachment.MarkFailed("Unsupported file type.");
                results.Add(failedAttachment);
                continue;
            }

            if (file.SizeBytes > DocumentProcessor.MaxFileSizeBytes)
            {
                var tooLargeId = Guid.NewGuid();
                var tooLargeRelative = BuildRelativePath(conversationId, tooLargeId, safeName);
                var tooLarge = new Attachment(
                    messageId: messageId == Guid.Empty ? tooLargeId : messageId,
                    originalFileName: originalName,
                    fileName: safeName,
                    extension: extension,
                    mimeType: file.ContentType ?? "application/octet-stream",
                    sizeBytes: file.SizeBytes,
                    relativePath: tooLargeRelative,
                    id: tooLargeId,
                    createdBy: createdBy,
                    status: AttachmentStatus.Failed);
                tooLarge.MarkFailed("File exceeds the maximum allowed size (25 MB).");
                results.Add(tooLarge);
                continue;
            }

            var attachmentId = Guid.NewGuid();
            var relativePath = BuildRelativePath(conversationId, attachmentId, safeName);

            try
            {
                await using var stream = await file.OpenReadAsync(cancellationToken);
                await _fileStorage.StoreAsync(stream, relativePath, cancellationToken);
            }
            catch (Exception ex)
            {
                var storeFailed = new Attachment(
                    messageId: messageId == Guid.Empty ? attachmentId : messageId,
                    originalFileName: originalName,
                    fileName: safeName,
                    extension: extension,
                    mimeType: file.ContentType ?? "application/octet-stream",
                    sizeBytes: file.SizeBytes,
                    relativePath: relativePath,
                    id: attachmentId,
                    createdBy: createdBy,
                    status: AttachmentStatus.Failed);
                storeFailed.MarkFailed($"Failed to store file: {ex.Message}");
                results.Add(storeFailed);
                continue;
            }

            var attachment = new Attachment(
                messageId: messageId == Guid.Empty ? attachmentId : messageId,
                originalFileName: originalName,
                fileName: safeName,
                extension: extension,
                mimeType: file.ContentType ?? "application/octet-stream",
                sizeBytes: file.SizeBytes,
                relativePath: relativePath,
                id: attachmentId,
                createdBy: createdBy,
                status: AttachmentStatus.Stored);

            attachment.MarkProcessing(createdBy);

            var processResult = await _documentProcessor.ProcessAsync(attachment, cancellationToken);
            if (processResult.IsError)
            {
                attachment.MarkFailed(
                    processResult.FirstError.Description ?? "Document processing failed.",
                    createdBy);
            }
            else if (!processResult.Value.Success)
            {
                attachment.MarkFailed(
                    processResult.Value.Error ?? "Document processing failed.",
                    createdBy);
            }
            else
            {
                attachment.MarkReady(createdBy);
                // Stash extracted text in a sibling .extracted.txt for later AI context (optional cache)
                try
                {
                    var extractedPath = relativePath + ".extracted.txt";
                    var text = processResult.Value.ExtractedText ?? string.Empty;
                    await using var es = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
                    await _fileStorage.StoreAsync(es, extractedPath, cancellationToken);
                }
                catch
                {
                    // non-fatal
                }
            }

            results.Add(attachment);
        }

        return results;
    }

    public async Task<ErrorOr<DocumentProcessingResult>> ReprocessAsync(
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        attachment.MarkProcessing();
        var result = await _documentProcessor.ProcessAsync(attachment, cancellationToken);
        if (result.IsError)
        {
            attachment.MarkFailed(result.FirstError.Description ?? "Reprocess failed.");
            return result.Errors;
        }

        if (!result.Value.Success)
        {
            attachment.MarkFailed(result.Value.Error ?? "Reprocess failed.");
            return result.Value;
        }

        attachment.MarkReady();
        return result.Value;
    }

    private static string BuildRelativePath(Guid conversationId, Guid attachmentId, string safeFileName) =>
        Path.Combine(conversationId.ToString("N"), attachmentId.ToString("N"), safeFileName);
}
