using FluentAssertions;
using RockAI.Common.Tests.Builders;
using RockAI.Domain.Conversations;

namespace RockAI.Domain.Tests.Conversations;

public sealed class ConversationTests
{
    [Fact]
    public void MarkAsCompleted_SetsCompletionStateAndTimestamp()
    {
        var conversation = new ConversationBuilder().Build();

        conversation.MarkAsCompleted();

        conversation.IsCompleted.Should().BeTrue();
        conversation.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateConversation_WhenTitleIsBlank_ReturnsInvalidTitle()
    {
        var conversation = new ConversationBuilder().Build();

        var result = conversation.UpdateConversation(" ", ConversationType.General, false);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ConversationErrors.InvalidTitle);
    }
}
