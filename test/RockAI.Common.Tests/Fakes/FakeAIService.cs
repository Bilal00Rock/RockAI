using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace RockAI.Common.Tests.Fakes;

/// <summary>
/// Deterministic AI service for unit tests. Supports controllable chunk streaming,
/// cancellation, and mid-stream failures without calling a real model.
/// </summary>
public sealed class FakeAIService : IAIService
{
    private readonly IReadOnlyList<string> _chunks;
    private readonly Exception? _exceptionAfterChunks;
    private readonly TimeSpan _delayBetweenChunks;
    private readonly Channel<string>? _liveChannel;

    public FakeAIService(
        IEnumerable<string>? chunks = null,
        Exception? exceptionAfterChunks = null,
        TimeSpan? delayBetweenChunks = null)
    {
        _chunks = chunks?.ToList() ?? ["Hello", " world"];
        _exceptionAfterChunks = exceptionAfterChunks;
        _delayBetweenChunks = delayBetweenChunks ?? TimeSpan.Zero;
    }

    /// <summary>
    /// Live controllable stream: writer pushes chunks; complete or fault from the test.
    /// </summary>
    public FakeAIService(Channel<string> liveChannel)
    {
        _liveChannel = liveChannel;
        _chunks = Array.Empty<string>();
        _exceptionAfterChunks = null;
        _delayBetweenChunks = TimeSpan.Zero;
    }

    public int GenerateStreamingCallCount { get; private set; }
    public AIChatRequest? LastChatRequest { get; private set; }

    public Task<ErrorOr<string>> GenerateAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = string.Concat(_chunks);
        return Task.FromResult<ErrorOr<string>>(text);
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(
        AIChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        GenerateStreamingCallCount++;
        LastChatRequest = request;

        if (_liveChannel is not null)
        {
            await foreach (var chunk in _liveChannel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        foreach (var chunk in _chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_delayBetweenChunks > TimeSpan.Zero)
                await Task.Delay(_delayBetweenChunks, cancellationToken);
            yield return chunk;
        }

        if (_exceptionAfterChunks is not null)
            throw _exceptionAfterChunks;
    }

    public Task<string> TestConnectionAsync() => Task.FromResult("ok");
}
