using RockAI.Application.Common.Models;

namespace RockAI.Application.Common.Interfaces;

public interface IFilePickerService
{
    /// <summary>
    /// Opens the platform file picker. Supports multiple selection when the platform allows it.
    /// Returns an empty list if the user cancels.
    /// </summary>
    Task<IReadOnlyList<PickedFile>> PickFilesAsync(
        IEnumerable<string>? allowedExtensions = null,
        CancellationToken cancellationToken = default);
}
