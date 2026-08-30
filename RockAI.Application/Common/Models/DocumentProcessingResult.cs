namespace RockAI.Application.Common.Models;

public sealed class DocumentProcessingResult
{
    public bool Success { get; init; }
    public string? ExtractedText { get; init; }
    public string DocumentType { get; init; } = "Unknown";
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    public string? Error { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static DocumentProcessingResult Succeeded(
        string extractedText,
        string documentType,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<string>? warnings = null) =>
        new()
        {
            Success = true,
            ExtractedText = extractedText,
            DocumentType = documentType,
            Metadata = metadata ?? new Dictionary<string, string>(),
            Warnings = warnings ?? Array.Empty<string>()
        };

    public static DocumentProcessingResult Failed(string error, string documentType = "Unknown") =>
        new()
        {
            Success = false,
            Error = error,
            DocumentType = documentType
        };
}
