namespace RockAI.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Stores the stream under a sanitized relative path inside the attachments root.
    /// Returns the relative path that was actually used.
    /// </summary>
    Task<string> StoreAsync(
        Stream content,
        string relativePath,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    bool Exists(string relativePath);

    string GetFullPath(string relativePath);

    /// <summary>
    /// Sanitizes a file name to prevent path traversal and illegal characters.
    /// </summary>
    string SanitizeFileName(string fileName);
}
