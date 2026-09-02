using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;

namespace RockAI.App.Services.Files;

public sealed class MauiFilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<PickedFile>> PickFilesAsync(
        IEnumerable<string>? allowedExtensions = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Build a permissive type filter. On Windows, overly strict custom maps
        // often prevent the dialog from opening — prefer broad document types.
        var extensions = (allowedExtensions ?? DefaultExtensions())
            .Select(e => e.StartsWith('.') ? e : "." + e)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        FilePickerFileType? fileTypes = null;
        try
        {
            fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, extensions },
                { DevicePlatform.macOS, extensions },
                { DevicePlatform.MacCatalyst, extensions },
                { DevicePlatform.iOS, extensions },
                { DevicePlatform.Android, extensions },
            });
        }
        catch
        {
            fileTypes = null; // fall back to unfiltered picker
        }

        IEnumerable<FileResult>? results = null;

        try
        {
            // Try multi-select first
            results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Select documents for RockAI",
                FileTypes = fileTypes
            });
        }
        catch (Exception)
        {
            // Some platforms throw when multi-select is unavailable — fall back to single pick
            results = null;
        }

        if (results is null || !results.Any())
        {
            try
            {
                var single = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a document for RockAI",
                    FileTypes = fileTypes
                });
                if (single is not null)
                    results = new[] { single };
            }
            catch (Exception)
            {
                // Last resort: no filter at all
                try
                {
                    var single = await FilePicker.Default.PickAsync(new PickOptions
                    {
                        PickerTitle = "Select a document for RockAI"
                    });
                    if (single is not null)
                        results = new[] { single };
                }
                catch
                {
                    return Array.Empty<PickedFile>();
                }
            }
        }

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
                if (!string.IsNullOrEmpty(fileResult.FullPath) && File.Exists(fileResult.FullPath))
                    size = new FileInfo(fileResult.FullPath).Length;
            }
            catch
            {
                // ignore
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

    private static IEnumerable<string> DefaultExtensions() =>
    [
        ".txt", ".md", ".markdown",
        ".json", ".csv", ".xml",
        ".cs", ".js", ".ts", ".tsx", ".jsx", ".py",
        ".html", ".css", ".xaml", ".sql",
        ".pdf",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"
    ];
}
