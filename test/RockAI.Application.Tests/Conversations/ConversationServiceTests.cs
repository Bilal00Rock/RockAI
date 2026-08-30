using FluentAssertions;
using NSubstitute;
using RockAI.Application.Authentication;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Conversations;
using RockAI.Common.Tests.Builders;
using RockAI.Common.Tests.Fakes;
using RockAI.Domain.Conversations;

namespace RockAI.Application.Tests.Conversations;

public sealed class ConversationServiceTests
{
    [Fact]
    public async Task CreateConversationAsync_WhenSessionIsAuthenticated_AddsAndCommitsConversation()
    {
        var conversations = Substitute.For<IConversationsRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var session = new TestUserSession().AuthenticatedAs(Guid.NewGuid());
        var service = new ConversationService(conversations, unitOfWork, session);

        var result = await service.CreateConversationAsync("A new conversation");

        result.IsError.Should().BeFalse();
        result.Value.UserId.Should().Be(session.UserId!.Value);
        result.Value.Title.Should().Be("A new conversation");
        await conversations.Received(1).AddConversationAsync(
            Arg.Is<Conversation>(conversation => conversation.Title == "A new conversation"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task CreateConversationAsync_WhenSessionIsNotAuthenticated_ReturnsNotAuthenticated()
    {
        var conversations = Substitute.For<IConversationsRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var session = new TestUserSession();
        var service = new ConversationService(conversations, unitOfWork, session);

        var result = await service.CreateConversationAsync("A new conversation");

        result.FirstError.Should().Be(AuthenticationErrors.NotAuthenticated);
        await conversations.DidNotReceive().AddConversationAsync(
            Arg.Any<Conversation>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitChangesAsync();
    }
    [Fact]
    public async Task DeleteConversationAsync_WhenOwned_DeletesAndCommits()
    {
        var userId = Guid.NewGuid();
        var conversation = new ConversationBuilder().ForUser(userId).Build();
        var conversations = Substitute.For<IConversationsRepository>();
        conversations.GetByIdAsync(conversation.Id, userId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var session = new TestUserSession().AuthenticatedAs(userId);
        var service = new ConversationService(conversations, unitOfWork, session);

        var result = await service.DeleteConversationAsync(conversation.Id);

        result.IsError.Should().BeFalse();
        await conversations.Received(1).DeleteAsync(conversation, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitChangesAsync();
    }
    [Fact]
    public async Task CreateConversationAsync_WhenTitleIsBlank_ReturnsInvalidTitle()
    {
        var conversations = Substitute.For<IConversationsRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var session = new TestUserSession().AuthenticatedAs(Guid.NewGuid());
        var service = new ConversationService(conversations, unitOfWork, session);

        var result = await service.CreateConversationAsync(" ");

        result.FirstError.Should().Be(ConversationErrors.InvalidTitle);
    }
}
