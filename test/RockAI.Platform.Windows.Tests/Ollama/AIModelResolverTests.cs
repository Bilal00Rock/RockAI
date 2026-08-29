using FluentAssertions;
using RockAI.Application.Common.Enums;
using RockAI.Platform.Windows.Services;

namespace RockAI.Platform.Windows.Tests.Ollama;

public sealed class AIModelResolverTests
{
    [Theory]
    [InlineData(nameof(AITask.Chat))]
    [InlineData(nameof(AITask.General))]
    public void ResolveModel_ForConfiguredTask_ReturnsConfiguredModel(string taskName)
    {
        var resolver = new AIModelResolver();
        var task = AITask.FromName(taskName);

        var model = resolver.ResolveModel(task);

        model.Should().Be("gemma3:4b");
    }
}
