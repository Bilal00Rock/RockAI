using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Common.Models;
using RockAI.Application.Documents;
using RockAI.Domain.Attachments;
using RockAI.Infrastructure.Documents.Extractors;
using RockAI.Infrastructure.Storage;

namespace RockAI.Application.Tests.Documents;

public class DocumentProcessorTests
{
    private readonly string _tempRoot;
    private readonly IFileStorageService _storage;
    private readonly IDocumentProcessor _processor;

    public DocumentProcessorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "RockAI-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _storage = new LocalFileStorageService(_tempRoot);

        var extractors = new IFileContentExtractor[]
        {
            new PlainTextExtractor(),
            new MarkdownExtractor(),
            new SourceCodeExtractor(),
            new StructuredTextExtractor()
        };

        _processor = new DocumentProcessor(_storage, extractors);
    }

    [Fact]
    public void IsSupported_KnownExtensions_ReturnsTrue()
    {
        Assert.True(_processor.IsSupported("txt"));
        Assert.True(_processor.IsSupported("md"));
        Assert.True(_processor.IsSupported("cs"));
        Assert.True(_processor.IsSupported("json"));
        Assert.True(_processor.IsSupported("csv"));
    }

    [Fact]
    public void IsSupported_UnknownExtension_ReturnsFalse()
    {
        Assert.False(_processor.IsSupported("exe"));
        Assert.False(_processor.IsSupported("docx"));
    }

    [Fact]
    public async Task ProcessAsync_TxtFile_ExtractsText()
    {
        var relative = "conv1/att1/hello.txt";
        await using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Hello RockAI")))
        {
            await _storage.StoreAsync(ms, relative);
        }

        var attachment = new Attachment(
            messageId: Guid.NewGuid(),
            originalFileName: "hello.txt",
            fileName: "hello.txt",
            extension: "txt",
            mimeType: "text/plain",
            sizeBytes: 12,
            relativePath: relative);

        var result = await _processor.ProcessAsync(attachment);

        Assert.False(result.IsError);
        Assert.True(result.Value.Success);
        Assert.Equal("PlainText", result.Value.DocumentType);
        Assert.Contains("Hello RockAI", result.Value.ExtractedText);
    }

    [Fact]
    public async Task ProcessAsync_Markdown_ExtractsText()
    {
        var relative = "conv1/att2/notes.md";
        var content = "# Title\n\nSome **markdown** content.";
        await using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)))
        {
            await _storage.StoreAsync(ms, relative);
        }

        var attachment = new Attachment(
            messageId: Guid.NewGuid(),
            originalFileName: "notes.md",
            fileName: "notes.md",
            extension: "md",
            mimeType: "text/markdown",
            sizeBytes: content.Length,
            relativePath: relative);

        var result = await _processor.ProcessAsync(attachment);

        Assert.False(result.IsError);
        Assert.True(result.Value.Success);
        Assert.Equal("Markdown", result.Value.DocumentType);
        Assert.Contains("# Title", result.Value.ExtractedText);
    }

    [Fact]
    public async Task ProcessAsync_Unsupported_ReturnsError()
    {
        var relative = "conv1/att3/bin.exe";
        await using (var ms = new MemoryStream(new byte[] { 0x00, 0x01 }))
        {
            await _storage.StoreAsync(ms, relative);
        }

        var attachment = new Attachment(
            messageId: Guid.NewGuid(),
            originalFileName: "bin.exe",
            fileName: "bin.exe",
            extension: "exe",
            mimeType: "application/octet-stream",
            sizeBytes: 2,
            relativePath: relative);

        var result = await _processor.ProcessAsync(attachment);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == "Attachment.UnsupportedFileType");
    }

    [Fact]
    public async Task ProcessAsync_EmptyFile_SucceedsWithEmptyText()
    {
        // Whitespace-only files succeed with empty extract (attachment flow must not hard-fail).
        var relative = "conv1/att4/empty.txt";
        await using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("   \n  ")))
        {
            await _storage.StoreAsync(ms, relative);
        }

        var attachment = new Attachment(
            messageId: Guid.NewGuid(),
            originalFileName: "empty.txt",
            fileName: "empty.txt",
            extension: "txt",
            mimeType: "text/plain",
            sizeBytes: 4,
            relativePath: relative);

        var result = await _processor.ProcessAsync(attachment);

        Assert.False(result.IsError);
        Assert.True(result.Value.Success);
        Assert.True(string.IsNullOrWhiteSpace(result.Value.ExtractedText));
    }

    [Fact]
    public async Task ProcessAsync_Json_PrettyPrints()
    {
        var relative = "conv1/att5/data.json";
        var json = "{\"name\":\"RockAI\",\"version\":1}";
        await using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
        {
            await _storage.StoreAsync(ms, relative);
        }

        var attachment = new Attachment(
            messageId: Guid.NewGuid(),
            originalFileName: "data.json",
            fileName: "data.json",
            extension: "json",
            mimeType: "application/json",
            sizeBytes: json.Length,
            relativePath: relative);

        var result = await _processor.ProcessAsync(attachment);

        Assert.False(result.IsError);
        Assert.True(result.Value.Success);
        Assert.Equal("Json", result.Value.DocumentType);
        Assert.Contains("RockAI", result.Value.ExtractedText);
    }
}
