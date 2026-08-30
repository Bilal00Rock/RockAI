using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RockAI.Domain.Attachments;

namespace RockAI.App.ViewModels;

public sealed class AttachmentChipViewModel : INotifyPropertyChanged
{
    private string _statusText = string.Empty;
    private bool _isBusy;

    public Guid Id { get; }
    public string FileName { get; }
    public string Extension { get; }
    public long SizeBytes { get; }
    public string SizeText { get; }
    public AttachmentStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>Underlying domain attachment once created/processed; null while still pending pick.</summary>
    public Attachment? Attachment { get; set; }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public bool CanRemove { get; set; } = true;

    public ICommand? RemoveCommand { get; set; }

    public AttachmentChipViewModel(string fileName, string extension, long sizeBytes, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        FileName = fileName;
        Extension = extension;
        SizeBytes = sizeBytes;
        SizeText = FormatSize(sizeBytes);
        Status = AttachmentStatus.Selected;
        StatusText = string.Empty;
    }

    public AttachmentChipViewModel(Attachment attachment)
    {
        Id = attachment.Id;
        FileName = attachment.OriginalFileName;
        Extension = attachment.Extension;
        SizeBytes = attachment.SizeBytes;
        SizeText = FormatSize(attachment.SizeBytes);
        Attachment = attachment;
        ApplyStatus(attachment.Status, attachment.ErrorMessage);
        CanRemove = false; // persisted message chips are display-only
    }

    public void ApplyStatus(AttachmentStatus status, string? error = null)
    {
        Status = status;
        ErrorMessage = error;
        IsBusy = status == AttachmentStatus.Processing || status == AttachmentStatus.Stored;
        StatusText = status.Name switch
        {
            nameof(AttachmentStatus.Selected) => string.Empty,
            nameof(AttachmentStatus.Stored) => "Storing…",
            nameof(AttachmentStatus.Processing) => "Processing…",
            nameof(AttachmentStatus.Ready) => "Ready",
            nameof(AttachmentStatus.Failed) => error ?? "Failed",
            _ => status.Name
        };
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(ErrorMessage));
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024.0):0.#} MB";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
