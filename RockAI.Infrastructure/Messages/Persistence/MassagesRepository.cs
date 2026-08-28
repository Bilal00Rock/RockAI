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

    public async Task AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _dbContext.Messages.AddAsync(message, cancellationToken);
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<List<Message>> ListByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Messages
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(Message message, CancellationToken cancellationToken = default)
    {
        _dbContext.Messages.Update(message);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message is not null)
        {
            _dbContext.Messages.Remove(message);
        }

        return Task.CompletedTask;
    }
}