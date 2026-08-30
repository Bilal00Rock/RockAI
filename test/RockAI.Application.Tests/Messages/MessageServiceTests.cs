using FluentAssertions;
using NSubstitute;
using RockAI.Application.Authentication;
using RockAI.Application.Common.Interfaces;
using RockAI.Application.Messages;
using RockAI.Common.Tests.Builders;
using RockAI.Common.Tests.Fakes;
using RockAI.Domain.Conversations;
using RockAI.Domain.Messages;

namespace RockAI.Application.Tests.Messages;

public sealed class MessageServiceTests
{
    private readonly IConversationsRepository _conversations = Substitute.For<IConversationsRepository>();
    private readonly IMessagesRepository _messages = Substitute.For<IMessagesRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly TestUserSession _session;

    public MessageServiceTests()
    {
        _session = new TestUserSession().AuthenticatedAs(_userId);
    }

    private MessageService CreateSut() =>
        new(_conversations, _messages, _unitOfWork, _session);

    private Conversation OwnedConversation(Guid? id = null) =>
        new ConversationBuilder()
            .ForUser(_userId)
            .WithId(id ?? Guid.NewGuid())
            .Build();

    private void SetupOwnedConversation(Conversation conversation)
    {
        _conversations.GetByIdAsync(conversation.Id, _userId, Arg.Any<CancellationToken>())
            .Returns(conversation);
    }

    // ---------- SendMessageAsync ----------

    [Fact]
    public async Task SendMessageAsync_WhenValid_AddsUserMessageAndCommits()
    {
        var conversation = OwnedConversation();
        SetupOwnedConversation(conversation);
        _messages.ListByConversationIdAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Message> { new MessageBuilder().ForConversation(conversation.Id).Build() });
        var sut = CreateSut();

        var result = await sut.SendMessageAsync(conversation.Id, "Hello world");

        result.IsError.Should().BeFalse();
        result.Value.Message.MessageRole.Should().Be(MessageRole.User);
        result.Value.Message.Content.Should().Be("Hello world");
        result.Value.Message.ConversationId.Should().Be(conversation.Id);
        result.Value.NewTitle.Should().BeNull();
        await _messages.Received(1).AddMessageAsync(
            Arg.Is<Message>(m => m.Content == "Hello world" && m.MessageRole == MessageRole.User),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task SendMessageAsync_WhenFirstMessage_GeneratesAndPersistsTitle()
    {
        var conversation = OwnedConversation();
        SetupOwnedConversation(conversation);
        _messages.ListByConversationIdAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Message>());
        var sut = CreateSut();

        var result = await sut.SendMessageAsync(conversation.Id, "Can you explain quantum computing?");

        result.IsError.Should().BeFalse();
        result.Value.NewTitle.Should().NotBeNullOrWhiteSpace();
        result.Value.NewTitle.Should().NotStartWith("Can you ");
        await _conversations.Received(1).UpdateAsync(conversation, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task SendMessageAsync_WhenContentIsEmptyOrWhitespace_ReturnsInvalidContent(string content)
    {
        var conversation = OwnedConversation();
        SetupOwnedConversation(conversation);
        var sut = CreateSut();

        var result = await sut.SendMessageAsync(conversation.Id, content);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(MessageErrors.InvalidContent);
        await _messages.DidNotReceive().AddMessageAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitChangesAsync();
    }

    [Fact]
    public async Task SendMessageAsync_WhenConversationNotFound_ReturnsNotFound()
    {
        var conversationId = Guid.NewGuid();
        _conversations.GetByIdAsync(conversationId, _userId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        var sut = CreateSut();

        var result = await sut.SendMessageAsync(conversationId, "Hello");

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ConversationErrors.NotFound);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotAuthenticated_ReturnsNotAuthenticated()
    {
        var session = new TestUserSession();
        var sut = new MessageService(_conversations, _messages, _unitOfWork, session);

        var result = await sut.SendMessageAsync(Guid.NewGuid(), "Hello");

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(AuthenticationErrors.NotAuthenticated);
    }

    // ---------- GetMessagesAsync ----------

    [Fact]
    public async Task GetMessagesAsync_WhenOwned_ReturnsOrderedMessages()
    {
        var conversation = OwnedConversation();
        SetupOwnedConversation(conversation);
        var msgs = new List<Message>
        {
            new MessageBuilder().ForConversation(conversation.Id).WithContent("a").Build(),
            new MessageBuilder().ForConversation(conversation.Id).WithContent("b").Build()
        };
        _messages.ListByConversationIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(msgs);
        var sut = CreateSut();

        var result = await sut.GetMessagesAsync(conversation.Id);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMessagesAsync_WhenConversationNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _conversations.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((Conversation?)null);
        var sut = CreateSut();

        var result = await sut.GetMessagesAsync(id);

        result.FirstError.Should().Be(ConversationErrors.NotFound);
    }

    // ---------- CreateAssistantMessageAsync ----------

    [Fact]
    public async Task CreateAssistantMessageAsync_WhenOwned_CreatesStreamingMessageByDefault()
    {
        var conversation = OwnedConversation();
        SetupOwnedConversation(conversation);
        var sut = CreateSut();

        var result = await sut.CreateAssistantMessageAsync(conversation.Id);

        result.IsError.Should().BeFalse();
        result.Value.MessageRole.Should().Be(MessageRole.Assistant);
        result.Value.Content.Should().BeEmpty();
        result.Value.Status.Should().Be(MessageStatus.Streaming);
        await _messages.Received(1).AddMessageAsync(
            Arg.Is<Message>(m => m.MessageRole == MessageRole.Assistant && m.Status == MessageStatus.Streaming),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task CreateAssistantMessageAsync_WithCustomStatus_UsesProvidedStatus()
    {
        var conversation = OwnedConversation();
        SetupOwnedConversation(conversation);
        var sut = CreateSut();

        var result = await sut.CreateAssistantMessageAsync(
            conversation.Id,
            content: "partial",
            status: MessageStatus.Failed);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(MessageStatus.Failed);
        result.Value.Content.Should().Be("partial");
    }

    [Fact]
    public async Task CreateAssistantMessageAsync_WhenNotOwned_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _conversations.GetByIdAsync(id, _userId, Arg.Any<CancellationToken>()).Returns((Conversation?)null);
        var sut = CreateSut();

        var result = await sut.CreateAssistantMessageAsync(id);

        result.FirstError.Should().Be(ConversationErrors.NotFound);
    }

    // ---------- UpdateMessageAsync ----------

    [Fact]
    public async Task UpdateMessageAsync_WhenOwned_UpdatesContentStatusAndCommits()
    {
        var conversation = OwnedConversation();
        var message = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Streaming)
            .WithContent("")
            .Build();
        _messages.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);
        SetupOwnedConversation(conversation);
        var sut = CreateSut();

        var result = await sut.UpdateMessageAsync(
            message.Id,
            "final answer",
            MessageRole.Assistant,
            MessageStatus.Completed);

        result.IsError.Should().BeFalse();
        result.Value.Content.Should().Be("final answer");
        result.Value.Status.Should().Be(MessageStatus.Completed);
        await _messages.Received(1).UpdateAsync(message, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task UpdateMessageAsync_WhenMessageNotFound_ReturnsNotFound()
    {
        _messages.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Message?)null);
        var sut = CreateSut();

        var result = await sut.UpdateMessageAsync(
            Guid.NewGuid(), "x", MessageRole.User, MessageStatus.Completed);

        result.FirstError.Should().Be(MessageErrors.NotFound);
    }

    [Fact]
    public async Task UpdateMessageAsync_WhenConversationNotOwned_ReturnsConversationNotFound()
    {
        var message = new MessageBuilder().Build();
        _messages.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);
        _conversations.GetByIdAsync(message.ConversationId, _userId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        var sut = CreateSut();

        var result = await sut.UpdateMessageAsync(
            message.Id, "x", MessageRole.User, MessageStatus.Completed);

        result.FirstError.Should().Be(ConversationErrors.NotFound);
    }

    // ---------- EditMessageContentAsync ----------

    [Fact]
    public async Task EditMessageContentAsync_WhenUserMessage_UpdatesContentAndRemovesLaterMessages()
    {
        var conversation = OwnedConversation();
        var t0 = DateTime.UtcNow.AddMinutes(-2);
        var t1 = DateTime.UtcNow.AddMinutes(-1);
        var t2 = DateTime.UtcNow;
        var userMsg = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .WithContent("old")
            .CreatedAt(t0)
            .Build();
        var assistant = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.Assistant)
            .WithContent("reply")
            .WithStatus(MessageStatus.Completed)
            .CreatedAt(t1)
            .Build();
        var laterUser = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .WithContent("later")
            .CreatedAt(t2)
            .Build();
        _messages.GetByIdAsync(userMsg.Id, Arg.Any<CancellationToken>()).Returns(userMsg);
        SetupOwnedConversation(conversation);
        _messages.ListByConversationIdAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Message> { userMsg, assistant, laterUser });
        var sut = CreateSut();

        var result = await sut.EditMessageContentAsync(userMsg.Id, "edited content");

        result.IsError.Should().BeFalse();
        result.Value.Content.Should().Be("edited content");
        await _messages.Received(1).UpdateAsync(userMsg, Arg.Any<CancellationToken>());
        await _messages.Received().DeleteAsync(assistant, Arg.Any<CancellationToken>());
        await _messages.Received().DeleteAsync(laterUser, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EditMessageContentAsync_WhenContentBlank_ReturnsInvalidContent(string content)
    {
        var sut = CreateSut();

        var result = await sut.EditMessageContentAsync(Guid.NewGuid(), content);

        result.FirstError.Should().Be(MessageErrors.InvalidContent);
        await _messages.DidNotReceive().UpdateAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditMessageContentAsync_WhenMessageIsStreaming_ReturnsCannotModifyWhileStreaming()
    {
        var conversation = OwnedConversation();
        var message = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .WithStatus(MessageStatus.Streaming)
            .Build();
        _messages.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);
        SetupOwnedConversation(conversation);
        var sut = CreateSut();

        var result = await sut.EditMessageContentAsync(message.Id, "new text");

        result.FirstError.Should().Be(MessageErrors.CannotModifyWhileStreaming);
        await _unitOfWork.DidNotReceive().CommitChangesAsync();
    }

    [Fact]
    public async Task EditMessageContentAsync_WhenMessageNotFound_ReturnsNotFound()
    {
        _messages.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Message?)null);
        var sut = CreateSut();

        var result = await sut.EditMessageContentAsync(Guid.NewGuid(), "content");

        result.FirstError.Should().Be(MessageErrors.NotFound);
    }

    [Fact]
    public async Task EditMessageContentAsync_WhenAssistantMessage_DoesNotDeleteLaterMessages()
    {
        var conversation = OwnedConversation();
        var assistant = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.Assistant)
            .WithContent("old reply")
            .WithStatus(MessageStatus.Completed)
            .CreatedAt(DateTime.UtcNow.AddMinutes(-1))
            .Build();
        var later = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .CreatedAt(DateTime.UtcNow)
            .Build();
        _messages.GetByIdAsync(assistant.Id, Arg.Any<CancellationToken>()).Returns(assistant);
        SetupOwnedConversation(conversation);
        var sut = CreateSut();

        var result = await sut.EditMessageContentAsync(assistant.Id, "new reply text");

        result.IsError.Should().BeFalse();
        await _messages.DidNotReceive().DeleteAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
        await _messages.DidNotReceive().ListByConversationIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ---------- DeleteMessageAsync ----------

    [Fact]
    public async Task DeleteMessageAsync_WhenUserMessage_DeletesMessageAndFollowingAssistant()
    {
        var conversation = OwnedConversation();
        var userMsg = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .Build();
        var assistant = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Completed)
            .Build();
        _messages.GetByIdAsync(userMsg.Id, Arg.Any<CancellationToken>()).Returns(userMsg);
        SetupOwnedConversation(conversation);
        _messages.ListByConversationIdAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Message> { userMsg, assistant });
        var sut = CreateSut();

        var result = await sut.DeleteMessageAsync(userMsg.Id);

        result.IsError.Should().BeFalse();
        await _messages.Received(1).DeleteAsync(userMsg, Arg.Any<CancellationToken>());
        await _messages.Received(1).DeleteAsync(assistant, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitChangesAsync();
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenAssistantMessage_DeletesOnlyThatMessage()
    {
        var conversation = OwnedConversation();
        var userMsg = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .Build();
        var assistant = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Completed)
            .Build();
        _messages.GetByIdAsync(assistant.Id, Arg.Any<CancellationToken>()).Returns(assistant);
        SetupOwnedConversation(conversation);
        _messages.ListByConversationIdAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Message> { userMsg, assistant });
        var sut = CreateSut();

        var result = await sut.DeleteMessageAsync(assistant.Id);

        result.IsError.Should().BeFalse();
        await _messages.Received(1).DeleteAsync(assistant, Arg.Any<CancellationToken>());
        await _messages.DidNotReceive().DeleteAsync(userMsg, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenOnlyMessage_DeletesIt()
    {
        var conversation = OwnedConversation();
        var only = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .Build();
        _messages.GetByIdAsync(only.Id, Arg.Any<CancellationToken>()).Returns(only);
        SetupOwnedConversation(conversation);
        _messages.ListByConversationIdAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Message> { only });
        var sut = CreateSut();

        var result = await sut.DeleteMessageAsync(only.Id);

        result.IsError.Should().BeFalse();
        await _messages.Received(1).DeleteAsync(only, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenStreaming_ReturnsCannotModifyWhileStreaming()
    {
        var conversation = OwnedConversation();
        var message = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.Assistant)
            .WithStatus(MessageStatus.Streaming)
            .WithContent("")
            .Build();
        _messages.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);
        SetupOwnedConversation(conversation);
        var sut = CreateSut();

        var result = await sut.DeleteMessageAsync(message.Id);

        result.FirstError.Should().Be(MessageErrors.CannotModifyWhileStreaming);
        await _unitOfWork.DidNotReceive().CommitChangesAsync();
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenNotFound_ReturnsNotFound()
    {
        _messages.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Message?)null);
        var sut = CreateSut();

        var result = await sut.DeleteMessageAsync(Guid.NewGuid());

        result.FirstError.Should().Be(MessageErrors.NotFound);
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenNotAuthenticated_ReturnsNotAuthenticated()
    {
        var sut = new MessageService(_conversations, _messages, _unitOfWork, new TestUserSession());

        var result = await sut.DeleteMessageAsync(Guid.NewGuid());

        result.FirstError.Should().Be(AuthenticationErrors.NotAuthenticated);
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenUserMessageHasNoFollowingAssistant_DeletesOnlyUser()
    {
        var conversation = OwnedConversation();
        var userMsg = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .Build();
        var anotherUser = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithRole(MessageRole.User)
            .Build();
        _messages.GetByIdAsync(userMsg.Id, Arg.Any<CancellationToken>()).Returns(userMsg);
        SetupOwnedConversation(conversation);
        _messages.ListByConversationIdAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Message> { userMsg, anotherUser });
        var sut = CreateSut();

        var result = await sut.DeleteMessageAsync(userMsg.Id);

        result.IsError.Should().BeFalse();
        await _messages.Received(1).DeleteAsync(userMsg, Arg.Any<CancellationToken>());
        await _messages.DidNotReceive().DeleteAsync(anotherUser, Arg.Any<CancellationToken>());
    }
}
