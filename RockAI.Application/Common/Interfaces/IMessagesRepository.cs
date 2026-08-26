using RockAI.Domain.Messages;

namespace RockAI.Application.Common.Interfaces;

public interface IMessagesRepository
{
    Task AddMessageAsync(Message message);
    Task<Message?> GetByIdAsync(Guid id);
    Task<List<Message>> ListByConversationIdAsync(Guid id);
    Task UpdateAsync(Message message);   
    Task DeleteAsync(Message message); 
}