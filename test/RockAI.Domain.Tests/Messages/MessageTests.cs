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
    public void UpdateMessage_WhenContentIsBlank_ReturnsInvalidContent()
    {
        var message = new MessageBuilder().Build();

        var result = message.UpdateMessage(" ", MessageRole.User, MessageStatus.Completed);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(MessageErrors.InvalidContent);
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
    public void UpdateMessage_WhenAssistantGenerationIsCancelled_PreservesPartialContent()
    {
        var message = new MessageBuilder()
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Streaming)
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
}
