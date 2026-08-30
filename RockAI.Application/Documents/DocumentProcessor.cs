using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Application.Documents;

public sealed class DocumentProcessor : IDocumentProcessor
{
    private readonly IFileStorageService _fileStorage;
    private readonly IReadOnlyList<IFileContentExtractor> _extractors;

    // Soft limit for this phase — large docs will fail with a clear message (RAG later).
    public const int MaxExtractedCharacters = 120_000;
    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "md", "markdown",
        "json", "csv", "xml",
        "cs", "js", "ts", "tsx", "jsx", "py",
        "html", "css", "xaml", "sql",
        "pdf" // registered for future; extractor may not be present yet
    };

    public DocumentProcessor(
        IFileStorageService fileStorage,
        IEnumerable<IFileContentExtractor> extractors)
    {
        _fileStorage = fileStorage;
        _extractors = extractors.ToList();
    }

    public IReadOnlyCollection<string> SupportedExtensions => Supported;

    public bool IsSupported(string extension, string? mimeType = null)
    {
        var ext = NormalizeExtension(extension);
        if (Supported.Contains(ext))
            return true;

        return _extractors.Any(e => e.CanHandle(ext, mimeType));
    }

    public async Task<ErrorOr<DocumentProcessingResult>> ProcessAsync(
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        if (attachment is null)
            return AttachmentErrors.NotFound;

        if (attachment.SizeBytes > MaxFileSizeBytes)
            return AttachmentErrors.FileTooLarge;

        var ext = NormalizeExtension(attachment.Extension);
        if (!IsSupported(ext, attachment.MimeType))
            return AttachmentErrors.UnsupportedFileType;

        if (!_fileStorage.Exists(attachment.RelativePath))
            return AttachmentErrors.FileNotFound;

        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(ext, attachment.MimeType));
        if (extractor is null)
            return AttachmentErrors.UnsupportedFileType;

        try
        {
            await using var stream = await _fileStorage.OpenReadAsync(attachment.RelativePath, cancellationToken);
            var result = await extractor.ExtractAsync(stream, attachment, cancellationToken);

            if (result.IsError)
                return result.Errors;

            var processing = result.Value;
            if (!processing.Success)
                return DocumentProcessingResult.Failed(
                    processing.Error ?? "Document processing failed.",
                    processing.DocumentType);

            // Empty extract is allowed (e.g. scanned PDF with no text layer); caller may still attach the file.
            var extracted = processing.ExtractedText ?? string.Empty;

            if (extracted.Length > MaxExtractedCharacters)
            {
                var truncated = extracted[..MaxExtractedCharacters];
                var warnings = processing.Warnings.ToList();
                warnings.Add(
                    $"Document was truncated to {MaxExtractedCharacters:N0} characters for this phase. Full RAG support is planned later.");

                return DocumentProcessingResult.Succeeded(
                    truncated,
                    processing.DocumentType,
                    processing.Metadata,
                    warnings);
            }

            return processing;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return DocumentProcessingResult.Failed(
                $"Failed to process document: {ex.Message}",
                GuessDocumentType(ext));
        }
    }

    private static string NormalizeExtension(string extension) =>
        (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();

    private static string GuessDocumentType(string ext) => ext switch
    {
        "md" or "markdown" => "Markdown",
        "json" => "Json",
        "xml" => "Xml",
        "csv" => "Csv",
        "pdf" => "Pdf",
        "cs" or "js" or "ts" or "tsx" or "jsx" or "py" or "html" or "css" or "xaml" or "sql" => "SourceCode",
        "txt" => "PlainText",
        _ => "Unknown"
    };
}
