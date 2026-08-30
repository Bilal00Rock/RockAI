using System.Text;
using System.Text.RegularExpressions;

namespace RockAI.App.Helpers;

/// <summary>
/// Minimal, streaming-tolerant Markdown parser for RockAI assistant messages.
/// Supports: paragraphs, headings (#-######), bold/italic (via FormattedString later),
/// unordered/ordered lists, blockquotes, fenced code blocks, links, inline code.
/// Incomplete fences during streaming become ordinary paragraphs so the UI never crashes.
/// </summary>
public static partial class MarkdownParser
{
    private static readonly Regex FenceStart = FenceStartRegex();
    private static readonly Regex Heading = HeadingRegex();
    private static readonly Regex UnorderedItem = UnorderedItemRegex();
    private static readonly Regex OrderedItem = OrderedItemRegex();
    private static readonly Regex Blockquote = BlockquoteRegex();
    private static readonly Regex Hr = HrRegex();

    public static IReadOnlyList<MarkdownBlock> Parse(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return Array.Empty<MarkdownBlock>();

        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var blocks = new List<MarkdownBlock>();
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            // Fenced code block
            var fenceMatch = FenceStart.Match(line);
            if (fenceMatch.Success)
            {
                var language = fenceMatch.Groups[1].Value.Trim();
                var code = new StringBuilder();
                i++;
                var closed = false;
                while (i < lines.Length)
                {
                    if (lines[i].StartsWith("```", StringComparison.Ordinal))
                    {
                        closed = true;
                        i++;
                        break;
                    }
                    if (code.Length > 0)
                        code.Append('\n');
                    code.Append(lines[i]);
                    i++;
                }

                if (closed)
                {
                    blocks.Add(new CodeBlock(language, code.ToString()));
                }
                else
                {
                    // Incomplete fence while streaming — treat accumulated text as paragraph
                    var incomplete = "```" + language;
                    if (code.Length > 0)
                        incomplete += "\n" + code;
                    blocks.Add(new ParagraphBlock(incomplete));
                }
                continue;
            }

            // Horizontal rule
            if (Hr.IsMatch(line))
            {
                blocks.Add(new HorizontalRuleBlock());
                i++;
                continue;
            }

            // Heading
            var headingMatch = Heading.Match(line);
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups[1].Value.Length;
                var text = headingMatch.Groups[2].Value.Trim();
                blocks.Add(new HeadingBlock(level, text));
                i++;
                continue;
            }

            // Blockquote (single-line for simplicity; consecutive lines merged)
            if (Blockquote.IsMatch(line))
            {
                var quote = new StringBuilder();
                while (i < lines.Length && Blockquote.IsMatch(lines[i]))
                {
                    var q = Blockquote.Match(lines[i]).Groups[1].Value;
                    if (quote.Length > 0)
                        quote.Append('\n');
                    quote.Append(q);
                    i++;
                }
                blocks.Add(new BlockquoteBlock(quote.ToString()));
                continue;
            }

            // Unordered list
            if (UnorderedItem.IsMatch(line))
            {
                var items = new List<string>();
                while (i < lines.Length && UnorderedItem.IsMatch(lines[i]))
                {
                    items.Add(UnorderedItem.Match(lines[i]).Groups[1].Value.Trim());
                    i++;
                }
                blocks.Add(new ListBlock(false, items));
                continue;
            }

            // Ordered list
            if (OrderedItem.IsMatch(line))
            {
                var items = new List<string>();
                while (i < lines.Length && OrderedItem.IsMatch(lines[i]))
                {
                    items.Add(OrderedItem.Match(lines[i]).Groups[1].Value.Trim());
                    i++;
                }
                blocks.Add(new ListBlock(true, items));
                continue;
            }

            // Blank line → skip (paragraph separator)
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Paragraph: collect consecutive non-special lines
            var para = new StringBuilder();
            while (i < lines.Length &&
                   !string.IsNullOrWhiteSpace(lines[i]) &&
                   !FenceStart.IsMatch(lines[i]) &&
                   !Heading.IsMatch(lines[i]) &&
                   !UnorderedItem.IsMatch(lines[i]) &&
                   !OrderedItem.IsMatch(lines[i]) &&
                   !Blockquote.IsMatch(lines[i]) &&
                   !Hr.IsMatch(lines[i]))
            {
                if (para.Length > 0)
                    para.Append(' ');
                para.Append(lines[i].Trim());
                i++;
            }
            if (para.Length > 0)
                blocks.Add(new ParagraphBlock(para.ToString()));
        }

        return blocks;
    }

    /// <summary>
    /// Parses inline Markdown (bold, italic, inline code, links) into a lightweight InlineFormattedString.
    /// This avoids constructing MAUI UI types during tests and streaming scenarios. Use ToFormattedString()
    /// on the result when running in a MAUI UI context.
    /// </summary>
    public static InlineFormattedString ParseInline(string text)
    {
        var fs = new InlineFormattedString();
        if (string.IsNullOrEmpty(text))
            return fs;

        // Simple sequential scanner for **bold**, *italic*, `code`, [text](url)
        var i = 0;
        var len = text.Length;
        var buffer = new StringBuilder();

        void Flush(FontAttributes attrs = FontAttributes.None, string? fontFamily = null, Color? color = null, TextDecorations? dec = null, Color? background = null)
        {
            if (buffer.Length == 0)
                return;
            fs.Spans.Add(new InlineSpan
            {
                Text = buffer.ToString(),
                FontAttributes = attrs,
                FontFamily = fontFamily,
                TextColor = color,
                TextDecorations = dec,
                BackgroundColor = background
            });
            buffer.Clear();
        }

        while (i < len)
        {
            // Inline code `...`
            if (text[i] == '`')
            {
                Flush();
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    var code = text.Substring(i + 1, end - i - 1);
                    fs.Spans.Add(new InlineSpan
                    {
                        Text = code,
                        FontFamily = "OpenSansRegular",
                        BackgroundColor = Color.FromArgb("#2D2D2D"),
                        TextColor = Color.FromArgb("#E6E6E6")
                    });
                    i = end + 1;
                    continue;
                }
            }

            // Bold **...**
            if (i + 1 < len && text[i] == '*' && text[i + 1] == '*')
            {
                Flush();
                var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (end > i)
                {
                    var bold = text.Substring(i + 2, end - i - 2);
                    fs.Spans.Add(new InlineSpan { Text = bold, FontAttributes = FontAttributes.Bold });
                    i = end + 2;
                    continue;
                }
            }

            // Italic *...*
            if (text[i] == '*' && (i + 1 >= len || text[i + 1] != '*'))
            {
                Flush();
                var end = text.IndexOf('*', i + 1);
                if (end > i)
                {
                    var italic = text.Substring(i + 1, end - i - 1);
                    fs.Spans.Add(new InlineSpan { Text = italic, FontAttributes = FontAttributes.Italic });
                    i = end + 1;
                    continue;
                }
            }

            // Link [text](url)
            if (text[i] == '[')
            {
                var closeBracket = text.IndexOf(']', i + 1);
                if (closeBracket > i && closeBracket + 1 < len && text[closeBracket + 1] == '(')
                {
                    var closeParen = text.IndexOf(')', closeBracket + 2);
                    if (closeParen > closeBracket)
                    {
                        Flush();
                        var linkText = text.Substring(i + 1, closeBracket - i - 1);
                        // URL is available but MAUI Span doesn't navigate; show as underlined text
                        fs.Spans.Add(new InlineSpan
                        {
                            Text = linkText,
                            TextDecorations = TextDecorations.Underline,
                            TextColor = Color.FromArgb("#4EA1FF")
                        });
                        i = closeParen + 1;
                        continue;
                    }
                }
            }

            buffer.Append(text[i]);
            i++;
        }

        Flush();
        return fs;
    }

    [GeneratedRegex(@"^```(\w*)\s*$", RegexOptions.Compiled)]
    private static partial Regex FenceStartRegex();

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\s*[-*+]\s+(.+)$", RegexOptions.Compiled)]
    private static partial Regex UnorderedItemRegex();

    [GeneratedRegex(@"^\s*\d+\.\s+(.+)$", RegexOptions.Compiled)]
    private static partial Regex OrderedItemRegex();

    [GeneratedRegex(@"^\s*>\s?(.*)$", RegexOptions.Compiled)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"^\s*(-{3,}|\*{3,}|_{3,})\s*$", RegexOptions.Compiled)]
    private static partial Regex HrRegex();
}
