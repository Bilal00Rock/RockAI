using System.Text;
using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Infrastructure.Documents.Extractors;

public sealed class PlainTextExtractor : IFileContentExtractor
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "log", "text"
    };

    public bool CanHandle(string extension, string? mimeType)
    {
        var ext = (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (Extensions.Contains(ext))
            return true;

        return mimeType is not null &&
               mimeType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ErrorOr<DocumentProcessingResult>> ExtractAsync(
        Stream content,
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
            return DocumentProcessingResult.Succeeded(string.Empty, "PlainText");

        return DocumentProcessingResult.Succeeded(
            text.Trim(),
            "PlainText",
            new Dictionary<string, string>
            {
                ["Encoding"] = reader.CurrentEncoding.WebName,
                ["CharacterCount"] = text.Length.ToString()
            });
    }
}
