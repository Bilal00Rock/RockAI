using FluentAssertions;
using RockAI.Application.Common.Models;
using RockAI.Platform.Windows.Services;
using RockAI.Platform.Windows.Services.OllamaAI;

namespace RockAI.Platform.Windows.Tests.Ollama;

public sealed class OllamaAIServiceTests
{
    [Fact]
    public async Task GenerateAsync_WhenPromptIsBlank_ReturnsPromptEmptyWithoutNetworkCall()
    {
        var service = new OllamaAIService(new AIModelResolver(), new OllamaEndpoints());

        var result = await service.GenerateAsync(new AIRequest
        {
            Model = "gemma3:4b",
            Prompt = " "
        });

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(OllamaErrors.PromptEmpty);
    }
}
