using RockAI.Application.Common.Enums;

namespace RockAI.Application.Common.Interfaces;

public interface IAIModelResolver
{
    string ResolveModel(AITask task);
}