using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Conversations;
using RockAI.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RockAI.Infrastructure.Conversations.Persistence;

public class ConversationsRepository : IConversationsRepository
{
    private readonly RockAIDbContext _dbContext;

    public ConversationsRepository(RockAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Conversations.AddAsync(conversation, cancellationToken);
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
    }

    public async Task<List<Conversation>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _dbContext.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        if (conversation is not null)
        {
            _dbContext.Conversations.Remove(conversation);
        }

        return Task.CompletedTask;
    }
}