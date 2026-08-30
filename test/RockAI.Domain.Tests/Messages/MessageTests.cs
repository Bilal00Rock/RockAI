using FluentAssertions;
using RockAI.Common.Tests.Builders;
using RockAI.Domain.Messages;

namespace RockAI.Domain.Tests.Messages;

public sealed class MessageTests
{
    [Fact]
    public void Constructor_DefaultsStatusToPending()
    {
        var message = new MessageBuilder().Build();

        message.Status.Should().Be(MessageStatus.Pending);
    }

    [Fact]
    public void Constructor_WhenUserContentIsBlank_AllowsEmptyForAttachments()
    {
        // Application layer validates that either text or attachments are present.
        // Domain allows empty user content so attachment-only messages can be created.
        var message = new Message(MessageRole.User, "  ", Guid.NewGuid());

        message.Content.Should().Be("  ");
        message.MessageRole.Should().Be(MessageRole.User);
    }

    [Fact]
    public void Constructor_WhenConversationIdIsEmpty_ThrowsArgumentException()
    {
        var action = () => new Message(MessageRole.User, "hi", Guid.Empty);

        action.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("conversationId");
    }

    [Fact]
    public void AssistantMessage_AllowsEmptyContentWhileStreaming()
    {
        var message = new Message(
            MessageRole.Assistant,
            string.Empty,
            Guid.NewGuid(),
            status: MessageStatus.Streaming);

        message.Status.Should().Be(MessageStatus.Streaming);
        message.Content.Should().BeEmpty();
    }

    [Fact]
    public void UpdateMessage_WhenContentIsBlankForUser_ReturnsInvalidContent()
    {
        var message = new MessageBuilder().Build();

        var result = message.UpdateMessage(" ", MessageRole.User, MessageStatus.Completed);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(MessageErrors.InvalidContent);
    }

    [Fact]
    public void UpdateMessage_WhenAssistantWithEmptyContent_Succeeds()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Streaming)
            .WithContent("")
            .Build();

        var result = message.UpdateMessage("", MessageRole.Assistant, MessageStatus.Cancelled);

        result.IsError.Should().BeFalse();
        message.Content.Should().BeEmpty();
        message.Status.Should().Be(MessageStatus.Cancelled);
    }

    [Fact]
    public void UpdateMessage_WhenAssistantGenerationIsCancelled_PreservesPartialContent()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Streaming)
            .WithContent("")
            .Build();

        var result = message.UpdateMessage(
            "partial response",
            MessageRole.Assistant,
            MessageStatus.Cancelled);

        result.IsError.Should().BeFalse();
        message.Content.Should().Be("partial response");
        message.Status.Should().Be(MessageStatus.Cancelled);
        message.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateMessage_WhenCompleted_SetsCompletedAt()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Streaming)
            .WithContent("")
            .Build();

        message.UpdateMessage("done", MessageRole.Assistant, MessageStatus.Completed);

        message.CompletedAt.Should().NotBeNull();
        message.Status.Should().Be(MessageStatus.Completed);
    }

    [Fact]
    public void UpdateMessage_WhenFailed_SetsCompletedAt()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Streaming)
            .WithContent("partial")
            .Build();

        message.UpdateMessage("partial", MessageRole.Assistant, MessageStatus.Failed);

        message.Status.Should().Be(MessageStatus.Failed);
        message.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateMessage_WhenBackToStreaming_ClearsCompletedAt()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Failed)
            .WithContent("old")
            .Build();
        message.UpdateMessage("old", MessageRole.Assistant, MessageStatus.Failed);

        message.UpdateMessage("", MessageRole.Assistant, MessageStatus.Streaming);

        message.Status.Should().Be(MessageStatus.Streaming);
        message.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_PreservesCustomIdAndCreatedAt()
    {
        var id = Guid.NewGuid();
        var created = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var message = new MessageBuilder()
            .WithId(id)
            .CreatedAt(created)
            .Build();

        message.Id.Should().Be(id);
        message.CreatedAt.Should().Be(created);
    }
}
