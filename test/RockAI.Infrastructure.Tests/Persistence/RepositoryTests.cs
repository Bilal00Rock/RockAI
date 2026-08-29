using FluentAssertions;
using RockAI.Common.Tests.Builders;
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
}
