using System.Text.RegularExpressions;
using RockAI.Application.Common.Interfaces;

namespace RockAI.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private static readonly Regex InvalidFileNameChars = new(
        @"[<>:""/\\|?*\x00-\x1F]",
        RegexOptions.Compiled);

    public LocalFileStorageService(string? rootPath = null)
    {
        // Default: under AppData/attachments when running in MAUI; allow override for tests.
        _rootPath = rootPath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RockAI",
                "attachments");

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> StoreAsync(
        Stream content,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var safeRelative = NormalizeRelativePath(relativePath);
        var fullPath = GetFullPath(safeRelative);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);
        return safeRelative;
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(NormalizeRelativePath(relativePath));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Attachment file not found.", fullPath);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(NormalizeRelativePath(relativePath));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        // Best-effort clean empty directories
        try
        {
            var dir = Path.GetDirectoryName(fullPath);
            while (!string.IsNullOrEmpty(dir) &&
                   dir.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase) &&
                   Directory.Exists(dir) &&
                   !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
                dir = Path.GetDirectoryName(dir);
            }
        }
        catch
        {
            // ignore cleanup failures
        }

        return Task.CompletedTask;
    }

    public bool Exists(string relativePath)
    {
        var fullPath = GetFullPath(NormalizeRelativePath(relativePath));
        return File.Exists(fullPath);
    }

    public string GetFullPath(string relativePath)
    {
        var safe = NormalizeRelativePath(relativePath);
        var full = Path.GetFullPath(Path.Combine(_rootPath, safe));
        if (!full.StartsWith(Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path traversal detected.");
        return full;
    }

    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        var name = Path.GetFileName(fileName.Trim());
        name = InvalidFileNameChars.Replace(name, "_");
        if (name is "." or "..")
            name = "file";

        // Limit length
        if (name.Length > 200)
        {
            var ext = Path.GetExtension(name);
            var baseName = Path.GetFileNameWithoutExtension(name);
            name = baseName[..Math.Min(180, baseName.Length)] + ext;
        }

        return name;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path cannot be empty.", nameof(relativePath));

        var normalized = relativePath
            .Replace('\\', '/')
            .TrimStart('/');

        // Prevent traversal
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s is "." or ".."))
            throw new InvalidOperationException("Path traversal detected.");

        return string.Join(Path.DirectorySeparatorChar, segments);
    }
}
