using ErrorOr;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Application.Common.Interfaces;

public interface IFileContentExtractor
{
    bool CanHandle(string extension, string? mimeType);

    Task<ErrorOr<DocumentProcessingResult>> ExtractAsync(
        Stream content,
        Attachment attachment,
        CancellationToken cancellationToken = default);
}
