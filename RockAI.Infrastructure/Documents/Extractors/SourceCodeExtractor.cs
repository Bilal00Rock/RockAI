using System.Text;
using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Infrastructure.Documents.Extractors;

public sealed class SourceCodeExtractor : IFileContentExtractor
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "cs", "js", "ts", "tsx", "jsx", "py",
        "html", "htm", "css", "xaml", "sql",
        "java", "go", "rs", "cpp", "c", "h", "hpp",
        "rb", "php", "swift", "kt", "scala"
    };

    public bool CanHandle(string extension, string? mimeType)
    {
        var ext = (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        return Extensions.Contains(ext);
    }

    public async Task<ErrorOr<DocumentProcessingResult>> ExtractAsync(
        Stream content,
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
            return DocumentProcessingResult.Succeeded(string.Empty, "SourceCode");

        var language = GuessLanguage(attachment.Extension);

        return DocumentProcessingResult.Succeeded(
            text.TrimEnd(),
            "SourceCode",
            new Dictionary<string, string>
            {
                ["Language"] = language,
                ["CharacterCount"] = text.Length.ToString(),
                ["LineCount"] = text.Split('\n').Length.ToString()
            });
    }

    private static string GuessLanguage(string extension) =>
        (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant() switch
        {
            "cs" => "csharp",
            "js" or "jsx" => "javascript",
            "ts" or "tsx" => "typescript",
            "py" => "python",
            "html" or "htm" => "html",
            "css" => "css",
            "xaml" => "xaml",
            "sql" => "sql",
            "java" => "java",
            "go" => "go",
            "rs" => "rust",
            "cpp" or "c" or "h" or "hpp" => "cpp",
            "rb" => "ruby",
            "php" => "php",
            "swift" => "swift",
            "kt" => "kotlin",
            "scala" => "scala",
            _ => "text"
        };
}
