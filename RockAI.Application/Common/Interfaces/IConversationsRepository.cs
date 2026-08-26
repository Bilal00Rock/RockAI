using RockAI.Domain.Conversations;

namespace RockAI.Application.Common.Interfaces;

public interface IConversationsRepository
{
    Task AddConversationAsync(Conversation conversation);
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<List<Conversation>> ListByUserIdAsync(Guid id);
    Task UpdateAsync(Conversation conversation);   
    Task DeleteAsync(Conversation conversation); 
}