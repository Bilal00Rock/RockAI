namespace RockAI.Application.Common.Models;

/// <summary>
/// Platform-agnostic representation of a file selected by the user.
/// The stream must be disposed by the caller after use.
/// </summary>
public sealed class PickedFile
{
    public required string FileName { get; init; }
    public string? ContentType { get; init; }
    public long SizeBytes { get; init; }
    public required Func<CancellationToken, Task<Stream>> OpenReadAsync { get; init; }
}
