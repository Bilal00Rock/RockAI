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
    private readonly IConversationsRepository _conversations = Substitute.For<IConversationsRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly TestUserSession _session;

    public ConversationServiceTests()
    {
        _session = new TestUserSession().AuthenticatedAs(_userId);
    }

    private ConversationService CreateSut() =>
        new(_conversations, _unitOfWork, _session);

    [Fact]
    public async Task CreateConversationAsync_WhenSessionIsAuthenticated_AddsAndCommitsConversation()
    {
        var sut = CreateSut();

        var result = await sut.CreateConversationAsync("A new conversation");

        result.IsError.Should().BeFalse();
        result.Value.UserId.Should().Be(_userId);
        result.Value.Title.Should().Be("A new conversation");
        await _conversations.Received(1).AddConversationAsync(
            Arg.Is<Conversation>(c => c.Title == "A new conversation"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task CreateConversationAsync_WhenSessionIsNotAuthenticated_ReturnsNotAuthenticated()
    {
        var sut = new ConversationService(_conversations, _unitOfWork, new TestUserSession());

        var result = await sut.CreateConversationAsync("A new conversation");

        result.FirstError.Should().Be(AuthenticationErrors.NotAuthenticated);
        await _conversations.DidNotReceive().AddConversationAsync(
            Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitChangesAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task CreateConversationAsync_WhenTitleIsBlank_ReturnsInvalidTitle(string title)
    {
        var sut = CreateSut();

        var result = await sut.CreateConversationAsync(title);

        result.FirstError.Should().Be(ConversationErrors.InvalidTitle);
    }

    [Fact]
    public async Task CreateConversationAsync_WithExplicitType_UsesProvidedType()
    {
        var sut = CreateSut();

        var result = await sut.CreateConversationAsync("Title", ConversationType.General);

        result.IsError.Should().BeFalse();
        result.Value.ConversationType.Should().Be(ConversationType.General);
    }

    [Fact]
    public async Task GetUserConversationsAsync_WhenAuthenticated_ReturnsList()
    {
        var list = new List<Conversation>
        {
            new ConversationBuilder().ForUser(_userId).Build(),
            new ConversationBuilder().ForUser(_userId).Build()
        };
        _conversations.ListByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(list);
        var sut = CreateSut();

        var result = await sut.GetUserConversationsAsync();

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserConversationsAsync_WhenNotAuthenticated_ReturnsNotAuthenticated()
    {
        var sut = new ConversationService(_conversations, _unitOfWork, new TestUserSession());

        var result = await sut.GetUserConversationsAsync();

        result.FirstError.Should().Be(AuthenticationErrors.NotAuthenticated);
    }

    [Fact]
    public async Task GetConversationAsync_WhenOwned_ReturnsConversation()
    {
        var conversation = new ConversationBuilder().ForUser(_userId).Build();
        _conversations.GetByIdAsync(conversation.Id, _userId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var sut = CreateSut();

        var result = await sut.GetConversationAsync(conversation.Id);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(conversation.Id);
    }

    [Fact]
    public async Task GetConversationAsync_WhenNotFound_ReturnsNotFound()
    {
        _conversations.GetByIdAsync(Arg.Any<Guid>(), _userId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        var sut = CreateSut();

        var result = await sut.GetConversationAsync(Guid.NewGuid());

        result.FirstError.Should().Be(ConversationErrors.NotFound);
    }

    [Fact]
    public async Task UpdateConversationAsync_WhenOwned_UpdatesAndCommits()
    {
        var conversation = new ConversationBuilder().ForUser(_userId).WithTitle("Old").Build();
        _conversations.GetByIdAsync(conversation.Id, _userId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var sut = CreateSut();

        var result = await sut.UpdateConversationAsync(
            conversation.Id, "New title", ConversationType.General, false);

        result.IsError.Should().BeFalse();
        result.Value.Title.Should().Be("New title");
        await _conversations.Received(1).UpdateAsync(conversation, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task UpdateConversationAsync_WhenTitleBlank_ReturnsInvalidTitle()
    {
        var conversation = new ConversationBuilder().ForUser(_userId).Build();
        _conversations.GetByIdAsync(conversation.Id, _userId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var sut = CreateSut();

        var result = await sut.UpdateConversationAsync(
            conversation.Id, "  ", ConversationType.General, false);

        result.FirstError.Should().Be(ConversationErrors.InvalidTitle);
        await _unitOfWork.DidNotReceive().CommitChangesAsync();
    }

    [Fact]
    public async Task UpdateConversationAsync_WhenNotFound_ReturnsNotFound()
    {
        _conversations.GetByIdAsync(Arg.Any<Guid>(), _userId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        var sut = CreateSut();

        var result = await sut.UpdateConversationAsync(
            Guid.NewGuid(), "Title", ConversationType.General, false);

        result.FirstError.Should().Be(ConversationErrors.NotFound);
    }

    [Fact]
    public async Task CompleteConversationAsync_WhenOwned_MarksCompletedAndCommits()
    {
        var conversation = new ConversationBuilder().ForUser(_userId).Build();
        _conversations.GetByIdAsync(conversation.Id, _userId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var sut = CreateSut();

        var result = await sut.CompleteConversationAsync(conversation.Id);

        result.IsError.Should().BeFalse();
        result.Value.IsCompleted.Should().BeTrue();
        result.Value.CompletedAt.Should().NotBeNull();
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task DeleteConversationAsync_WhenOwned_DeletesAndCommits()
    {
        var conversation = new ConversationBuilder().ForUser(_userId).Build();
        _conversations.GetByIdAsync(conversation.Id, _userId, Arg.Any<CancellationToken>())
            .Returns(conversation);
        var sut = CreateSut();

        var result = await sut.DeleteConversationAsync(conversation.Id);

        result.IsError.Should().BeFalse();
        await _conversations.Received(1).DeleteAsync(conversation, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task DeleteConversationAsync_WhenNotFound_ReturnsNotFound()
    {
        _conversations.GetByIdAsync(Arg.Any<Guid>(), _userId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        var sut = CreateSut();

        var result = await sut.DeleteConversationAsync(Guid.NewGuid());

        result.FirstError.Should().Be(ConversationErrors.NotFound);
        await _conversations.DidNotReceive().DeleteAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteConversationAsync_WhenNotAuthenticated_ReturnsNotAuthenticated()
    {
        var sut = new ConversationService(_conversations, _unitOfWork, new TestUserSession());

        var result = await sut.DeleteConversationAsync(Guid.NewGuid());

        result.FirstError.Should().Be(AuthenticationErrors.NotAuthenticated);
    }
}
