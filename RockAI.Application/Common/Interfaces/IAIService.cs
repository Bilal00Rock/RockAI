using ErrorOr;
using RockAI.Application.Common.Models;

namespace RockAI.Application.Common.Interfaces;

public interface IAIService
{
    Task<ErrorOr<string>> GenerateAsync(AIRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> GenerateStreamingAsync(AIChatRequest request, CancellationToken cancellationToken = default);
    Task<string> TestConnectionAsync();
}