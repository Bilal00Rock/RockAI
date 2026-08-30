using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;

namespace RockAI.App.Services.Files;

public sealed class MauiFilePickerService : IFilePickerService
{
    // Default set used when caller does not restrict types
    private static readonly string[] DefaultExtensions =
    {
        ".txt", ".md", ".markdown",
        ".json", ".csv", ".xml",
        ".cs", ".js", ".ts", ".tsx", ".jsx", ".py",
        ".html", ".css", ".xaml", ".sql",
        ".pdf"
    };

    public async Task<IReadOnlyList<PickedFile>> PickFilesAsync(
        IEnumerable<string>? allowedExtensions = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var extensions = (allowedExtensions ?? DefaultExtensions)
            .Select(e => e.StartsWith('.') ? e : "." + e)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, extensions },
            { DevicePlatform.macOS, extensions },
            { DevicePlatform.MacCatalyst, extensions },
            { DevicePlatform.iOS, extensions },
            { DevicePlatform.Android, extensions },
        });

        try
        {
            // Prefer multi-select when available
            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Select documents for RockAI",
                FileTypes = fileTypes
            });

            if (results is null || !results.Any())
                return Array.Empty<PickedFile>();

            var picked = new List<PickedFile>();
            foreach (var fileResult in results)
            {
                if (fileResult is null)
                    continue;

                long size = 0;
                try
                {
                    var fi = new FileInfo(fileResult.FullPath);
                    if (fi.Exists)
                        size = fi.Length;
                }
                catch
                {
                    // size remains 0
                }

                var localResult = fileResult;
                picked.Add(new PickedFile
                {
                    FileName = localResult.FileName,
                    ContentType = localResult.ContentType,
                    SizeBytes = size,
                    OpenReadAsync = async ct =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return await localResult.OpenReadAsync();
                    }
                });
            }

            return picked;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // User cancelled or platform denied — treat as empty selection
            return Array.Empty<PickedFile>();
        }
    }
}
