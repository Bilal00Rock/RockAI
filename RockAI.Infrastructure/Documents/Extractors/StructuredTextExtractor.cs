using System.Text;
using System.Text.Json;
using System.Xml;
using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Infrastructure.Documents.Extractors;

/// <summary>
/// Handles JSON, XML, and CSV by extracting a normalized text representation.
/// </summary>
public sealed class StructuredTextExtractor : IFileContentExtractor
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "json", "xml", "csv"
    };

    public bool CanHandle(string extension, string? mimeType)
    {
        var ext = (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (Extensions.Contains(ext))
            return true;

        if (mimeType is null)
            return false;

        return mimeType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("csv", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ErrorOr<DocumentProcessingResult>> ExtractAsync(
        Stream content,
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
            return DocumentProcessingResult.Succeeded(string.Empty, "Structured");

        var ext = (attachment.Extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        var warnings = new List<string>();

        return ext switch
        {
            "json" => ProcessJson(text, warnings),
            "xml" => ProcessXml(text, warnings),
            "csv" => ProcessCsv(text, warnings),
            _ => DocumentProcessingResult.Succeeded(text.Trim(), "Structured")
        };
    }

    private static DocumentProcessingResult ProcessJson(string text, List<string> warnings)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            // Re-serialize with indentation for better AI readability
            var options = new JsonSerializerOptions { WriteIndented = true };
            var pretty = JsonSerializer.Serialize(doc.RootElement, options);
            return DocumentProcessingResult.Succeeded(
                pretty,
                "Json",
                new Dictionary<string, string> { ["CharacterCount"] = pretty.Length.ToString() },
                warnings);
        }
        catch (JsonException ex)
        {
            warnings.Add($"JSON is not well-formed ({ex.Message}). Returning raw text.");
            return DocumentProcessingResult.Succeeded(
                text.Trim(),
                "Json",
                warnings: warnings);
        }
    }

    private static DocumentProcessingResult ProcessXml(string text, List<string> warnings)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(text);
            return DocumentProcessingResult.Succeeded(
                text.Trim(),
                "Xml",
                new Dictionary<string, string>
                {
                    ["RootElement"] = xmlDoc.DocumentElement?.Name ?? "",
                    ["CharacterCount"] = text.Length.ToString()
                },
                warnings);
        }
        catch (XmlException ex)
        {
            warnings.Add($"XML is not well-formed ({ex.Message}). Returning raw text.");
            return DocumentProcessingResult.Succeeded(text.Trim(), "Xml", warnings: warnings);
        }
    }

    private static DocumentProcessingResult ProcessCsv(string text, List<string> warnings)
    {
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var nonEmpty = lines.Count(l => !string.IsNullOrWhiteSpace(l));

        return DocumentProcessingResult.Succeeded(
            text.Trim(),
            "Csv",
            new Dictionary<string, string>
            {
                ["LineCount"] = nonEmpty.ToString(),
                ["CharacterCount"] = text.Length.ToString()
            },
            warnings);
    }
}
