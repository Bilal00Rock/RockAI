using System.Text;
using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace RockAI.Infrastructure.Documents.Extractors;

/// <summary>
/// Extracts text from text-based PDFs using PdfPig (managed, local-first).
/// Scanned / image-only PDFs will yield little or no text (OCR is a later phase).
/// </summary>
public sealed class PdfTextExtractor : IFileContentExtractor
{
    public bool CanHandle(string extension, string? mimeType)
    {
        var ext = (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (ext == "pdf")
            return true;

        return mimeType is not null &&
               mimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ErrorOr<DocumentProcessingResult>> ExtractAsync(
        Stream content,
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // PdfPig needs a seekable stream
            Stream working = content;
            MemoryStream? owned = null;
            if (!content.CanSeek)
            {
                owned = new MemoryStream();
                content.CopyTo(owned);
                owned.Position = 0;
                working = owned;
            }

            using (owned)
            using (var document = PdfDocument.Open(working))
            {
                var sb = new StringBuilder();
                var pageCount = document.NumberOfPages;
                var pagesWithText = 0;

                for (var i = 1; i <= pageCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var page = document.GetPage(i);
                    var pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        pagesWithText++;
                        if (sb.Length > 0)
                            sb.AppendLine().AppendLine($"--- Page {i} ---").AppendLine();
                        else
                            sb.AppendLine($"--- Page {i} ---").AppendLine();
                        sb.AppendLine(pageText.Trim());
                    }
                }

                var text = sb.ToString().Trim();
                var warnings = new List<string>();

                if (string.IsNullOrWhiteSpace(text))
                {
                    warnings.Add(
                        "No extractable text was found. This may be a scanned PDF or image-based document. OCR is not enabled in this phase.");
                    return Task.FromResult<ErrorOr<DocumentProcessingResult>>(
                        DocumentProcessingResult.Succeeded(
                            string.Empty,
                            "Pdf",
                            new Dictionary<string, string>
                            {
                                ["PageCount"] = pageCount.ToString(),
                                ["PagesWithText"] = "0"
                            },
                            warnings));
                }

                if (pagesWithText < pageCount)
                {
                    warnings.Add(
                        $"Text was extracted from {pagesWithText} of {pageCount} pages. Some pages may be image-only.");
                }

                return Task.FromResult<ErrorOr<DocumentProcessingResult>>(
                    DocumentProcessingResult.Succeeded(
                        text,
                        "Pdf",
                        new Dictionary<string, string>
                        {
                            ["PageCount"] = pageCount.ToString(),
                            ["PagesWithText"] = pagesWithText.ToString(),
                            ["CharacterCount"] = text.Length.ToString()
                        },
                        warnings));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult<ErrorOr<DocumentProcessingResult>>(
                DocumentProcessingResult.Failed(
                    $"PDF extraction failed: {ex.Message}",
                    "Pdf"));
        }
    }
}
