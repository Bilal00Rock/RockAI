using ErrorOr;
using RockAI.Domain.Conversations;

namespace RockAI.Application.Common.Interfaces;

public interface IConversationService
{
    Task<ErrorOr<Conversation>> CreateConversationAsync(
        string title,
        ConversationType? conversationType = null,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<List<Conversation>>> GetUserConversationsAsync(
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Conversation>> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Conversation>> UpdateConversationAsync(
        Guid conversationId,
        string title,
        ConversationType conversationType,
        bool isCompleted,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Conversation>> CompleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> DeleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}