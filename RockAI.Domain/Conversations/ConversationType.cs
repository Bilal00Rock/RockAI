using Ardalis.SmartEnum;

namespace RockAI.Domain.Conversations;

public class ConversationType : SmartEnum<ConversationType>
{
    public static readonly ConversationType General = new(nameof(General), 0);
    public static readonly ConversationType Work = new(nameof(Work), 1);
    public static readonly ConversationType Coding = new(nameof(Coding), 2);
    public static readonly ConversationType Personal = new(nameof(Personal), 3);
    public static readonly ConversationType Research = new(nameof(Research), 4);
    public static readonly ConversationType Planning = new(nameof(Planning), 5);
    public static readonly ConversationType Task = new(nameof(Task), 6);

    public ConversationType(string name, int value) : base(name, value)
    {
    }
}