using RockAI.Application.Common.Interfaces;

namespace RockAI.Platform.Windows.Services.OllamaAI;

public sealed class OllamaEndpoints : IAIEndpoints
{
    public Uri BaseAddress { get; } = new("http://127.0.0.1:11434/");
    public string Tags { get; } = "api/tags";
    public string Generate { get; } = "api/generate";
    public string Chat { get; } = "api/chat";
    public bool UseProxy { get; } = false;
}
