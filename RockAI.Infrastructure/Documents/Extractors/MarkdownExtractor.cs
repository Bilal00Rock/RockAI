using System.Text;
using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Infrastructure.Documents.Extractors;

public sealed class MarkdownExtractor : IFileContentExtractor
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "md", "markdown", "mdown", "mkd"
    };

    public bool CanHandle(string extension, string? mimeType)
    {
        var ext = (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        return Extensions.Contains(ext) ||
               (mimeType is not null && mimeType.Contains("markdown", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ErrorOr<DocumentProcessingResult>> ExtractAsync(
        Stream content,
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
            return DocumentProcessingResult.Succeeded(string.Empty, "Markdown");

        return DocumentProcessingResult.Succeeded(
            text.Trim(),
            "Markdown",
            new Dictionary<string, string>
            {
                ["CharacterCount"] = text.Length.ToString()
            });
    }
}
