using ErrorOr;

namespace RockAI.Platform.Windows.Services.OllamaAI;

public static class OllamaErrors
{
    public static readonly Error PromptEmpty = Error.Validation(
        code: "AI.Prompt.Empty",
        description: "AI prompt cannot be empty.");

    public static readonly Error ModelEmpty = Error.Validation(
        code: "AI.Model.Empty",
        description: "AI model must be specified.");

    public static readonly Error EmptyResponse = Error.Failure(
        code: "AI.Ollama.EmptyResponse",
        description: "Ollama returned an empty response.");

    public static readonly Error RequestCancelled = Error.Failure(
        code: "AI.Request.Cancelled",
        description: "The AI request was cancelled.");

    public static Error RequestFailed(string description) => Error.Failure(
        code: "AI.Ollama.RequestFailed",
        description: description);

    public static Error ConnectionFailed(string message) => Error.Failure(
        code: "AI.Ollama.ConnectionFailed",
        description: $"Could not connect to Ollama. Make sure Ollama is running. {message}");

    public static Error UnknownError(string message) => Error.Failure(
        code: "AI.Ollama.UnknownError",
        description: message);
}
