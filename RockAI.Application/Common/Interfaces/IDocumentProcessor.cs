using ErrorOr;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Application.Common.Interfaces;

public interface IDocumentProcessor
{
    /// <summary>
    /// Validates support, extracts content, normalizes, and returns a structured result.
    /// Does not mutate the attachment status — caller is responsible for MarkReady / MarkFailed.
    /// </summary>
    Task<ErrorOr<DocumentProcessingResult>> ProcessAsync(
        Attachment attachment,
        CancellationToken cancellationToken = default);

    bool IsSupported(string extension, string? mimeType = null);

    IReadOnlyCollection<string> SupportedExtensions { get; }
}
