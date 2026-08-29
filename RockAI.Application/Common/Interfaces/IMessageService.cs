using ErrorOr;
using RockAI.Application.Messages;
using RockAI.Domain.Messages;

namespace RockAI.Application.Common.Interfaces;

public interface IMessageService
{
    Task<ErrorOr<SendMessageResult>> SendMessageAsync(Guid conversationId, string content, CancellationToken cancellationToken = default);
    Task<ErrorOr<List<Message>>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<ErrorOr<Message>> UpdateMessageAsync(Guid messageId, string content, MessageRole messageRole, MessageStatus status, CancellationToken cancellationToken = default);
    Task<ErrorOr<Message>> CreateAssistantMessageAsync(
        Guid conversationId,
        string content = "",
        CancellationToken cancellationToken = default,
        MessageStatus? status = null);
}