using RockAI.Domain.Conversations;

namespace RockAI.Application.Common.Interfaces;

public interface IConversationsRepository
{
    Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<List<Conversation>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task DeleteAsync(Conversation conversation, CancellationToken cancellationToken = default);
}