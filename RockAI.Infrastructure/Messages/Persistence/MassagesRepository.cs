using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Messages;
using RockAI.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RockAI.Infrastructure.Messages.Persistence;

public class MessagesRepository : IMessagesRepository
{
    private readonly RockAIDbContext _dbContext;

    public MessagesRepository(RockAIDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddMessageAsync(Message message)
    {
        await _dbContext.Messages.AddAsync(message);
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Message>> ListByConversationIdAsync(Guid id)
    {
        return await _dbContext.Messages.Where(message => message.ConversationId == id).ToListAsync();
    }

    public Task UpdateAsync(Message message)
    {
        _dbContext.Messages.Update(message);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Message message)
    {
        if (message is not null)
        {
            _dbContext.Messages.Remove(message);
        }

        return Task.CompletedTask;
    }
}