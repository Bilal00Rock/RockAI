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

    public async Task AddConversationAsync(Conversation Conversation)
    {
        await _dbContext.Conversations.AddAsync(Conversation);
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Conversation>> ListByUserIdAsync(Guid id)
    {
        return await _dbContext.Conversations.Where(conversation => conversation.UserId == id).ToListAsync();
    }

    public Task UpdateAsync(Conversation conversation)
    {
        _dbContext.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Conversation conversation)
    {
        if (conversation is not null)
        {
            _dbContext.Conversations.Remove(conversation);
        }

        return Task.CompletedTask;
    }
}