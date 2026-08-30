using Ardalis.SmartEnum;

namespace RockAI.Domain.Attachments;

public class AttachmentStatus : SmartEnum<AttachmentStatus>
{
    public static readonly AttachmentStatus Selected   = new(nameof(Selected), 0);
    public static readonly AttachmentStatus Stored     = new(nameof(Stored), 1);
    public static readonly AttachmentStatus Processing = new(nameof(Processing), 2);
    public static readonly AttachmentStatus Ready      = new(nameof(Ready), 3);
    public static readonly AttachmentStatus Failed     = new(nameof(Failed), 4);

    public AttachmentStatus(string name, int value) : base(name, value)
    {
    }
}
