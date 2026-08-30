namespace RockAI.App.Helpers;

/// <summary>
/// Lightweight parsed Markdown blocks. Used by the UI renderer.
/// Streaming-safe: incomplete fences are emitted as plain Paragraph blocks.
/// </summary>
public abstract record MarkdownBlock;

public sealed record ParagraphBlock(string Text) : MarkdownBlock;

public sealed record HeadingBlock(int Level, string Text) : MarkdownBlock;

public sealed record ListBlock(bool Ordered, IReadOnlyList<string> Items) : MarkdownBlock;

public sealed record BlockquoteBlock(string Text) : MarkdownBlock;

public sealed record CodeBlock(string Language, string Code) : MarkdownBlock;

public sealed record HorizontalRuleBlock() : MarkdownBlock;
