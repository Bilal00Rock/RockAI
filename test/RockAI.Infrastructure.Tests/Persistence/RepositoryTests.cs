using FluentAssertions;
using RockAI.Common.Tests.Builders;
using RockAI.Domain.Messages;
using RockAI.Infrastructure.Conversations.Persistence;
using RockAI.Infrastructure.Messages.Persistence;
using RockAI.Infrastructure.Users.Persistence;

namespace RockAI.Infrastructure.Tests.Persistence;

public sealed class RepositoryTests
{
    [Fact]
    public async Task UsersRepository_PersistsAndFindsUserByEmail()
    {
        using var database = new InfrastructureTestDatabase();
        var repository = new UsersRepository(database.Context);
        var user = new UserBuilder().WithEmail("persisted@example.com").Build();

        await repository.AddUserAsync(user);
        await database.Context.SaveChangesAsync();

        var result = await repository.GetByEmailAsync("persisted@example.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task UsersRepository_GetByEmail_WhenMissing_ReturnsNull()
    {
        using var database = new InfrastructureTestDatabase();
        var repository = new UsersRepository(database.Context);

        var result = await repository.GetByEmailAsync("missing@example.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Repositories_PersistConversationAndMessagesWithSmartEnumValues()
    {
        using var database = new InfrastructureTestDatabase();
        var users = new UsersRepository(database.Context);
        var conversations = new ConversationsRepository(database.Context);
        var messages = new MessagesRepository(database.Context);
        var user = new UserBuilder().Build();
        var conversation = new ConversationBuilder().ForUser(user.Id).Build();
        var message = new MessageBuilder().ForConversation(conversation.Id).Build();

        await users.AddUserAsync(user);
        await conversations.AddConversationAsync(conversation);
        await messages.AddMessageAsync(message);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var result = await messages.ListByConversationIdAsync(conversation.Id);

        result.Should().ContainSingle().Which.Status.Should().Be(message.Status);
        (await conversations.GetByIdAsync(conversation.Id, user.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task ConversationsRepository_GetById_FiltersByUserId()
    {
        using var database = new InfrastructureTestDatabase();
        var users = new UsersRepository(database.Context);
        var conversations = new ConversationsRepository(database.Context);
        var owner = new UserBuilder().Build();
        var other = new UserBuilder().WithEmail("other@example.com").Build();
        var conversation = new ConversationBuilder().ForUser(owner.Id).Build();

        await users.AddUserAsync(owner);
        await users.AddUserAsync(other);
        await conversations.AddConversationAsync(conversation);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        (await conversations.GetByIdAsync(conversation.Id, owner.Id)).Should().NotBeNull();
        (await conversations.GetByIdAsync(conversation.Id, other.Id)).Should().BeNull();
    }

    [Fact]
    public async Task ConversationsRepository_ListByUserId_ReturnsOnlyOwnedOrderedByCreatedAtDesc()
    {
        using var database = new InfrastructureTestDatabase();
        var users = new UsersRepository(database.Context);
        var conversations = new ConversationsRepository(database.Context);
        var user = new UserBuilder().Build();
        var older = new ConversationBuilder()
            .ForUser(user.Id)
            .WithTitle("Older")
            .CreatedAt(DateTime.UtcNow.AddHours(-2))
            .Build();
        var newer = new ConversationBuilder()
            .ForUser(user.Id)
            .WithTitle("Newer")
            .CreatedAt(DateTime.UtcNow.AddHours(-1))
            .Build();

        await users.AddUserAsync(user);
        await conversations.AddConversationAsync(older);
        await conversations.AddConversationAsync(newer);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var list = await conversations.ListByUserIdAsync(user.Id);

        list.Should().HaveCount(2);
        list[0].Title.Should().Be("Newer");
        list[1].Title.Should().Be("Older");
    }

    [Fact]
    public async Task ConversationsRepository_Delete_RemovesConversation()
    {
        using var database = new InfrastructureTestDatabase();
        var users = new UsersRepository(database.Context);
        var conversations = new ConversationsRepository(database.Context);
        var user = new UserBuilder().Build();
        var conversation = new ConversationBuilder().ForUser(user.Id).Build();

        await users.AddUserAsync(user);
        await conversations.AddConversationAsync(conversation);
        await database.Context.SaveChangesAsync();

        await conversations.DeleteAsync(conversation);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        (await conversations.GetByIdAsync(conversation.Id, user.Id)).Should().BeNull();
    }

    [Fact]
    public async Task ConversationsRepository_Update_PersistsTitleChange()
    {
        using var database = new InfrastructureTestDatabase();
        var users = new UsersRepository(database.Context);
        var conversations = new ConversationsRepository(database.Context);
        var user = new UserBuilder().Build();
        var conversation = new ConversationBuilder().ForUser(user.Id).WithTitle("Before").Build();

        await users.AddUserAsync(user);
        await conversations.AddConversationAsync(conversation);
        await database.Context.SaveChangesAsync();

        conversation.UpdateConversation("After", conversation.ConversationType, false);
        await conversations.UpdateAsync(conversation);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var loaded = await conversations.GetByIdAsync(conversation.Id, user.Id);
        loaded!.Title.Should().Be("After");
    }

    [Fact]
    public async Task MessagesRepository_ListByConversationId_OrdersByCreatedAt()
    {
        using var database = new InfrastructureTestDatabase();
        var users = new UsersRepository(database.Context);
        var conversations = new ConversationsRepository(database.Context);
        var messages = new MessagesRepository(database.Context);
        var user = new UserBuilder().Build();
        var conversation = new ConversationBuilder().ForUser(user.Id).Build();
        var first = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithContent("first")
            .CreatedAt(DateTime.UtcNow.AddMinutes(-2))
            .Build();
        var second = new MessageBuilder()
            .ForConversation(conversation.Id)
            .WithContent("second")
            .CreatedAt(DateTime.UtcNow.AddMinutes(-1))
            .Build();

        await users.AddUserAsync(user);
        await conversations.AddConversationAsync(conversation);
        await messages.AddMessageAsync(first);
        await messages.AddMessageAsync(second);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var list = await messages.ListByConversationIdAsync(conversation.Id);

        list.Should().HaveCount(2);
        list[0].Content.Should().Be("first");
        list[1].Content.Should().Be("second");
    }

    [Fact]
    public async Task MessagesRepository_Update_PersistsStatusAndContent()
    {
        using var database = new InfrastructureTestDatabase();
        var users = new UsersRepository(database.Context);
        var conversations = new ConversationsRepository(database.Context);
        var messages = new MessagesRepository(database.Context);
        var user = new UserBuilder().Build();
        var conversation = new ConversationBuilder().ForUser(user.Id).Build();
        var message = new Message(
            MessageRole.Assistant,
            "",
            conversation.Id,
            status: MessageStatus.Streaming);

        await users.AddUserAsync(user);
        await conversations.AddConversationAsync(conversation);
        await messages.AddMessageAsync(message);
        await database.Context.SaveChangesAsync();

        message.UpdateMessage("final", MessageRole.Assistant, MessageStatus.Completed);
        await messages.UpdateAsync(message);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var loaded = await messages.GetByIdAsync(message.Id);
        loaded!.Content.Should().Be("final");
        loaded.Status.Should().Be(MessageStatus.Completed);
    }

    [Fact]
    public async Task MessagesRepository_Delete_RemovesMessage()
    {
        using var database = new InfrastructureTestDatabase();
        var users = new UsersRepository(database.Context);
        var conversations = new ConversationsRepository(database.Context);
        var messages = new MessagesRepository(database.Context);
        var user = new UserBuilder().Build();
        var conversation = new ConversationBuilder().ForUser(user.Id).Build();
        var message = new MessageBuilder().ForConversation(conversation.Id).Build();

        await users.AddUserAsync(user);
        await conversations.AddConversationAsync(conversation);
        await messages.AddMessageAsync(message);
        await database.Context.SaveChangesAsync();

        await messages.DeleteAsync(message);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        (await messages.GetByIdAsync(message.Id)).Should().BeNull();
        (await messages.ListByConversationIdAsync(conversation.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task MessagesRepository_GetById_WhenMissing_ReturnsNull()
    {
        using var database = new InfrastructureTestDatabase();
        var messages = new MessagesRepository(database.Context);

        (await messages.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }
}
