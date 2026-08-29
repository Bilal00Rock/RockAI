using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RockAI.Platform.Windows.Services.OllamaAI;

public sealed class OllamaAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IAIModelResolver _modelResolver;


    public OllamaAIService(IAIModelResolver modelResolver)
    {
        _modelResolver = modelResolver;

        var handler = new HttpClientHandler
        {
            UseProxy = false
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:11434/"),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }
    public async Task<string> TestConnectionAsync()
    {
        using var response = await _httpClient.GetAsync("api/tags");

        var body = await response.Content.ReadAsStringAsync();

        return $"Status: {(int)response.StatusCode} {response.ReasonPhrase}\n\n{body}";
    }
    public async Task<ErrorOr<string>> GenerateAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var model = string.IsNullOrWhiteSpace(request.Model) ? _modelResolver.ResolveModel(request.Task) : request.Model;

        if (string.IsNullOrWhiteSpace(request.Prompt))
            return OllamaErrors.PromptEmpty;

        if (string.IsNullOrWhiteSpace(request.Model))
            return OllamaErrors.ModelEmpty;

        var ollamaRequest = new OllamaGenerateRequest
        {
            Model = model,
            Prompt = request.Prompt,
            System = request.SystemPrompt,
            Stream = false,
            Options = new OllamaOptions
            {
                Temperature = request.Temperature,
                NumPredict = request.MaxTokens
            }
        };

        try
        {

            using var response = await _httpClient.PostAsJsonAsync(
                "api/generate",
                ollamaRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);

                return OllamaErrors.RequestFailed(
                    string.IsNullOrWhiteSpace(error)
                        ? $"Ollama returned HTTP {(int)response.StatusCode}."
                        : error);
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
                    cancellationToken: cancellationToken);

            if (result is null || string.IsNullOrWhiteSpace(result.Response))
            {
                return OllamaErrors.EmptyResponse;
            }

            return result.Response;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return OllamaErrors.RequestCancelled;
        }
        catch (HttpRequestException ex)
        {
            return OllamaErrors.ConnectionFailed(ex.Message);
        }
        catch (Exception ex)
        {
            return OllamaErrors.UnknownError(ex.Message);
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingAsync(AIChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request.Messages.Count == 0)
            throw new ArgumentException(
                "AI request must contain at least one message.",
                nameof(request));

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? _modelResolver.ResolveModel(request.Task)
            : request.Model;

        var ollamaRequest = new OllamaChatRequest
        {
            Model = model,
            Messages = request.Messages
                .Select(message => new OllamaMessage
                {
                    Role = message.Role.Name.ToLowerInvariant(),
                    Content = message.Content
                })
                .ToList(),
            Stream = true,
            Options = new OllamaOptions
            {
                Temperature = request.Temperature,
                NumPredict = request.MaxTokens
            }
        };

        using var requestMessage = new HttpRequestMessage(
            HttpMethod.Post,
            "api/chat")
        {
            Content = JsonContent.Create(ollamaRequest)
        };

        using var response = await _httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            OllamaChatResponse? chunk;

            try
            {
                chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            }
            catch (JsonException)
            {
                continue;
            }

            if (chunk is null)
                continue;

            if (!string.IsNullOrEmpty(chunk.Message?.Content))
                yield return chunk.Message.Content;

            if (chunk.Done)
                break;
        }
    }
    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; init; } = string.Empty;

        [JsonPropertyName("system")]
        public string? System { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; init; }
    }

    private sealed class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; init; }

        [JsonPropertyName("num_predict")]
        public int? NumPredict { get; init; }
    }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; init; }
    }
    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; init; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; init; }
    }
    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; init; }

        [JsonPropertyName("done")]
        public bool Done { get; init; }
    }

}