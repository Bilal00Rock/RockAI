using RockAI.Application.Common.Enums;
using RockAI.Application.Common.Interfaces;

namespace RockAI.Platform.Windows.Services;

public sealed class AIModelResolver : IAIModelResolver
{
    private readonly Dictionary<AITask, string> _models = new()
    {
        [AITask.Chat] = "gemma3:4b",
        [AITask.MasterDataExtraction] = "gemma3:4b",
        [AITask.CVExtraction] = "gemma3:4b",
        [AITask.ImageAnalysis] = "gemma3:4b",
        [AITask.General] = "gemma3:4b"
    };

    public string ResolveModel(AITask task)
    {
        if (_models.TryGetValue(task, out var model))
            return model;

        throw new InvalidOperationException(
            $"No AI model configured for task '{task.Name}'.");
    }
}