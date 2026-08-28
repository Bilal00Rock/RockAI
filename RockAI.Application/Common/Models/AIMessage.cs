using RockAI.Application.Common.Enums;

namespace RockAI.Application.Common.Models;

public sealed class AIMessage
{
    public AIMessageRole Role { get; init; } = AIMessageRole.User;

    public string Content { get; init; } = string.Empty;
}