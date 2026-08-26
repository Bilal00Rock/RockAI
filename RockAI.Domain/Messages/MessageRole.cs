using Ardalis.SmartEnum;

namespace RockAI.Domain.Messages;

public class MessageRole : SmartEnum<MessageRole>
{
    public static readonly MessageRole User = new(nameof(User), 0);
    public static readonly MessageRole Assistant = new(nameof(Assistant), 1);
    public static readonly MessageRole System = new(nameof(System), 2);
    public static readonly MessageRole Tool = new(nameof(Tool), 3);

    public MessageRole(string name, int value) : base(name, value)
    {
    }
}