using RockAI.Domain.Messages;

namespace RockAI.Application.Common.Interfaces;

public interface IMessagesRepository
{
    Task AddMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Message>> ListByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Message message, CancellationToken cancellationToken = default);
    Task DeleteAsync(Message message, CancellationToken cancellationToken = default);
}