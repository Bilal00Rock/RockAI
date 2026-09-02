using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Domain.Attachments;

namespace RockAI.Infrastructure.Documents.Extractors;

public sealed class ImageExtractor : IFileContentExtractor
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "png", "jpg", "jpeg", "gif", "webp", "bmp"
    };

    public bool CanHandle(string extension, string? mimeType)
    {
        var ext = (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        return Extensions.Contains(ext) ||
               (mimeType is not null && mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ErrorOr<DocumentProcessingResult>> ExtractAsync(
        Stream content,
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var header = new byte[12];
        var bytesRead = 0;
        while (bytesRead < header.Length)
        {
            var read = await content.ReadAsync(header.AsMemory(bytesRead, header.Length - bytesRead), cancellationToken);
            if (read == 0)
                break;

            bytesRead += read;
        }

        if (!IsRecognizedImage(header, bytesRead))
        {
            return DocumentProcessingResult.Failed(
                "The file does not contain a recognized image format.",
                "Image");
        }

        return DocumentProcessingResult.Succeeded(
            string.Empty,
            "Image",
            new Dictionary<string, string>
            {
                ["MimeType"] = attachment.MimeType,
                ["Extension"] = attachment.Extension,
                ["SizeBytes"] = attachment.SizeBytes.ToString()
            });
    }

    private static bool IsRecognizedImage(byte[] header, int length)
    {
        if (length >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return true;

        if (length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return true;

        if (length >= 4 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8')
            return true;

        if (length >= 2 && header[0] == 'B' && header[1] == 'M')
            return true;

        return length >= 12 &&
               header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
               header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P';
    }
}
