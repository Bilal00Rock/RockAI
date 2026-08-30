using FluentAssertions;
using RockAI.App.Helpers;

namespace RockAI.App.Tests.Helpers;

public class MarkdownParserTests
{
    [Fact]
    public void Parse_Empty_ReturnsEmpty()
    {
        MarkdownParser.Parse(null).Should().BeEmpty();
        MarkdownParser.Parse("").Should().BeEmpty();
        MarkdownParser.Parse("   \n  ").Should().BeEmpty();
    }

    [Fact]
    public void Parse_Paragraph()
    {
        var blocks = MarkdownParser.Parse("Hello world.");
        blocks.Should().HaveCount(1);
        blocks[0].Should().BeOfType<ParagraphBlock>()
            .Which.Text.Should().Be("Hello world.");
    }

    [Fact]
    public void Parse_Headings()
    {
        var md = """
            # H1
            ## H2
            ### H3
            """;
        var blocks = MarkdownParser.Parse(md);
        blocks.Should().HaveCount(3);
        blocks[0].Should().BeOfType<HeadingBlock>().Which.Level.Should().Be(1);
        blocks[1].Should().BeOfType<HeadingBlock>().Which.Level.Should().Be(2);
        blocks[2].Should().BeOfType<HeadingBlock>().Which.Level.Should().Be(3);
    }

    [Fact]
    public void Parse_UnorderedList()
    {
        var md = """
            - Easier testing
            - Better maintainability
            """;
        var blocks = MarkdownParser.Parse(md);
        blocks.Should().HaveCount(1);
        var list = blocks[0].Should().BeOfType<ListBlock>().Subject;
        list.Ordered.Should().BeFalse();
        list.Items.Should().Equal("Easier testing", "Better maintainability");
    }

    [Fact]
    public void Parse_OrderedList()
    {
        var md = """
            1. First
            2. Second
            """;
        var blocks = MarkdownParser.Parse(md);
        var list = blocks[0].Should().BeOfType<ListBlock>().Subject;
        list.Ordered.Should().BeTrue();
        list.Items.Should().Equal("First", "Second");
    }

    [Fact]
    public void Parse_Blockquote()
    {
        var blocks = MarkdownParser.Parse("> quoted text");
        blocks[0].Should().BeOfType<BlockquoteBlock>()
            .Which.Text.Should().Be("quoted text");
    }

    [Fact]
    public void Parse_FencedCodeBlock()
    {
        var md = """
            ```csharp
            builder.Services.AddSingleton<IMyService, MyService>();
            ```
            """;
        var blocks = MarkdownParser.Parse(md);
        blocks.Should().HaveCount(1);
        var code = blocks[0].Should().BeOfType<CodeBlock>().Subject;
        code.Language.Should().Be("csharp");
        code.Code.Should().Contain("AddSingleton");
        code.Code.Should().NotContain("```");
    }

    [Fact]
    public void Parse_IncompleteFence_DuringStreaming_BecomesParagraph()
    {
        // Streaming intermediate state: fence opened but not closed
        var md = """
            ```csh
            var result =
            """;
        var blocks = MarkdownParser.Parse(md);
        blocks.Should().NotBeEmpty();
        // Must not throw and should produce a paragraph (or incomplete content), never crash
        blocks.Should().ContainSingle(b => b is ParagraphBlock);
    }

    [Fact]
    public void Parse_CompleteExample()
    {
        var md = """
            # Dependency Injection

            Dependency injection reduces coupling.

            - Easier testing
            - Better maintainability

            ```csharp
            builder.Services.AddSingleton<IMyService, MyService>();
            ```
            """;
        var blocks = MarkdownParser.Parse(md);
        blocks.Should().Contain(x => x.GetType() == typeof(HeadingBlock));
        blocks.Should().Contain(x => x.GetType() == typeof(ParagraphBlock));
        blocks.Should().Contain(x => x.GetType() == typeof(ListBlock));

        blocks.Should().Contain(x => x.GetType() == typeof(CodeBlock) && ((CodeBlock)x).Language == "csharp");
    }

    [Fact]
    public void ParseInline_BoldItalicCode()
    {
        var fs = MarkdownParser.ParseInline("Hello **bold** and *italic* and `code`");
        fs.Spans.Should().NotBeEmpty();
        fs.Spans.Should().Contain(s => s.FontAttributes == FontAttributes.Bold && s.Text == "bold");
        fs.Spans.Should().Contain(s => s.FontAttributes == FontAttributes.Italic && s.Text == "italic");
        fs.Spans.Should().Contain(s => s.Text == "code");
    }

    [Fact]
    public void ParseInline_Link()
    {
        var fs = MarkdownParser.ParseInline("See [docs](https://example.com)");
        fs.Spans.Should().Contain(s => s.Text == "docs" && s.TextDecorations == TextDecorations.Underline);
    }

    [Fact]
    public void ParseInline_Incomplete_DoesNotThrow()
    {
        // Incomplete markdown during streaming
        var act = () => MarkdownParser.ParseInline("**not closed and `open");
        act.Should().NotThrow();
    }
}
