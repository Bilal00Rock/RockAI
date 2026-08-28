using RockAI.Application.Common.Enums;

namespace RockAI.Application.Common.Models;

public sealed class AIRequest
{
    public AITask Task { get; init; } = AITask.General;
    public string Prompt { get; init; } = string.Empty;
    public string? SystemPrompt { get; init; }
    public string? Model { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}

public sealed class AIChatRequest
{
    public AITask Task { get; init; } = AITask.General;

    public string? Model { get; init; }

    public IReadOnlyList<AIMessage> Messages { get; init; } = [];

    public double? Temperature { get; init; }

    public int? MaxTokens { get; init; }
}