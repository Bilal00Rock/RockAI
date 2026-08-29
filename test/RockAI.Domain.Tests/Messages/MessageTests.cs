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
}
