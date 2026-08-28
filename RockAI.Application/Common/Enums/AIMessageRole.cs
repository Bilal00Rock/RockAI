using Ardalis.SmartEnum;

namespace RockAI.Application.Common.Enums;

public sealed class AIMessageRole : SmartEnum<AIMessageRole>
{
    public static readonly AIMessageRole System =
        new(nameof(System), 1);

    public static readonly AIMessageRole User =
        new(nameof(User), 2);

    public static readonly AIMessageRole Assistant =
        new(nameof(Assistant), 3);

    private AIMessageRole(string name, int value)
        : base(name, value)
    {
    }
}