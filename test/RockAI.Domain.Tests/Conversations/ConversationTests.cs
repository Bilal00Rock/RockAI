using FluentAssertions;
using RockAI.Common.Tests.Builders;
using RockAI.Domain.Conversations;

namespace RockAI.Domain.Tests.Conversations;

public sealed class ConversationTests
{
    [Fact]
    public void Constructor_WhenTitleIsBlank_ThrowsArgumentException()
    {
        var action = () => new Conversation(ConversationType.General, "  ", Guid.NewGuid());

        action.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("title");
    }

    [Fact]
    public void Constructor_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        var action = () => new Conversation(ConversationType.General, "Title", Guid.Empty);

        action.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("userId");
    }

    [Fact]
    public void Constructor_DefaultsIsCompletedToFalse()
    {
        var conversation = new ConversationBuilder().Build();

        conversation.IsCompleted.Should().BeFalse();
        conversation.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsCompleted_SetsCompletionStateAndTimestamp()
    {
        var conversation = new ConversationBuilder().Build();

        conversation.MarkAsCompleted();

        conversation.IsCompleted.Should().BeTrue();
        conversation.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsIncomplete_ClearsCompletionState()
    {
        var conversation = new ConversationBuilder().Completed().Build();

        conversation.MarkAsIncomplete();

        conversation.IsCompleted.Should().BeFalse();
        conversation.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateConversation_WhenTitleIsBlank_ReturnsInvalidTitle()
    {
        var conversation = new ConversationBuilder().Build();

        var result = conversation.UpdateConversation(" ", ConversationType.General, false);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ConversationErrors.InvalidTitle);
    }

    [Fact]
    public void UpdateConversation_WhenValid_UpdatesTitleAndType()
    {
        var conversation = new ConversationBuilder().WithTitle("Old").Build();

        var result = conversation.UpdateConversation("New title", ConversationType.General, false);

        result.IsError.Should().BeFalse();
        conversation.Title.Should().Be("New title");
        conversation.ConversationType.Should().Be(ConversationType.General);
    }

    [Fact]
    public void UpdateConversation_WhenCompleting_SetsCompletedState()
    {
        var conversation = new ConversationBuilder().Build();

        var result = conversation.UpdateConversation("Title", ConversationType.General, true);

        result.IsError.Should().BeFalse();
        conversation.IsCompleted.Should().BeTrue();
        conversation.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateConversation_WhenUncompleting_ClearsCompletedState()
    {
        var conversation = new ConversationBuilder().Completed().Build();

        var result = conversation.UpdateConversation("Title", ConversationType.General, false);

        result.IsError.Should().BeFalse();
        conversation.IsCompleted.Should().BeFalse();
        conversation.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_PreservesCustomId()
    {
        var id = Guid.NewGuid();
        var conversation = new ConversationBuilder().WithId(id).Build();

        conversation.Id.Should().Be(id);
    }
}
