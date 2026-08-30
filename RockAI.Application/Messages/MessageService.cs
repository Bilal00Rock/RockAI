using ErrorOr;
using RockAI.Application.Authentication;
using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Conversations;
using RockAI.Domain.Messages;

namespace RockAI.Application.Messages;

public sealed class MessageService : IMessageService
{
    private readonly IConversationsRepository _conversationsRepository;
    private readonly IMessagesRepository _messagesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;

    public MessageService(
        IConversationsRepository conversationsRepository,
        IMessagesRepository messagesRepository,
        IUnitOfWork unitOfWork,
        IUserSession userSession)
    {
        _conversationsRepository = conversationsRepository;
        _messagesRepository = messagesRepository;
        _unitOfWork = unitOfWork;
        _userSession = userSession;
    }

    public async Task<ErrorOr<SendMessageResult>> SendMessageAsync(
        Guid conversationId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var conversationResult = await GetOwnedConversationAsync(conversationId, cancellationToken);
        if (conversationResult.IsError)
            return conversationResult.Errors;

        if (string.IsNullOrWhiteSpace(content))
            return MessageErrors.InvalidContent;

        var existingMessages = await _messagesRepository.ListByConversationIdAsync(
            conversationId,
            cancellationToken);

        Message message;
        try
        {
            message = new Message(MessageRole.User, content, conversationId);
        }
        catch (ArgumentException)
        {
            return MessageErrors.InvalidContent;
        }

        await _messagesRepository.AddMessageAsync(message, cancellationToken);
        string? newTitle = null;
        if (existingMessages.Count == 0)
        {
            newTitle = CreateTitle(content);
            var updateResult = conversationResult.Value.UpdateConversation(
                newTitle,
                conversationResult.Value.ConversationType,
                conversationResult.Value.IsCompleted);

            if (updateResult.IsError)
                return updateResult.Errors;

            await _conversationsRepository.UpdateAsync(
                conversationResult.Value,
                cancellationToken);
        }

        await _unitOfWork.CommitChangesAsync();
        return new SendMessageResult(
        message,
        newTitle);
    }

    public async Task<ErrorOr<List<Message>>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversationResult = await GetOwnedConversationAsync(conversationId, cancellationToken);
        if (conversationResult.IsError)
            return conversationResult.Errors;

        return await _messagesRepository.ListByConversationIdAsync(conversationId, cancellationToken);
    }

    public async Task<ErrorOr<Message>> UpdateMessageAsync(
        Guid messageId,
        string content,
        MessageRole messageRole,
        MessageStatus status,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        var message = await _messagesRepository.GetByIdAsync(messageId, cancellationToken);
        if (message is null)
            return MessageErrors.NotFound;

        var conversation = await _conversationsRepository.GetByIdAsync(
            message.ConversationId,
            userIdResult.Value,
            cancellationToken);
        if (conversation is null)
            return ConversationErrors.NotFound;

        var updateResult = message.UpdateMessage(content, messageRole, status);
        if (updateResult.IsError)
            return updateResult.Errors;

        await _messagesRepository.UpdateAsync(message, cancellationToken);
        await _unitOfWork.CommitChangesAsync();
        return message;
    }
    public async Task<ErrorOr<Message>> CreateAssistantMessageAsync(
        Guid conversationId,
        string content = "",
        CancellationToken cancellationToken = default,
        MessageStatus? status = null)
    {
        var conversationResult = await GetOwnedConversationAsync(conversationId, cancellationToken);

        if (conversationResult.IsError)
            return conversationResult.Errors;

        var message = new Message(
            MessageRole.Assistant,
            content,
            conversationId,
            status: status ?? MessageStatus.Streaming);

        await _messagesRepository.AddMessageAsync(message, cancellationToken);

        await _unitOfWork.CommitChangesAsync();

        return message;
    }
    public async Task<ErrorOr<Message>> EditMessageContentAsync(Guid messageId, string content, CancellationToken cancellationToken = default)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        if (string.IsNullOrWhiteSpace(content))
            return MessageErrors.InvalidContent;

        var message = await _messagesRepository.GetByIdAsync(messageId, cancellationToken);
        if (message is null)
            return MessageErrors.NotFound;

        var conversation = await _conversationsRepository.GetByIdAsync(
            message.ConversationId,
            userIdResult.Value,
            cancellationToken);
        if (conversation is null)
            return ConversationErrors.NotFound;

        if (message.Status == MessageStatus.Streaming)
            return MessageErrors.CannotModifyWhileStreaming;

        var updateResult = message.UpdateMessage(content.Trim(), message.MessageRole, message.Status);
        if (updateResult.IsError)
            return updateResult.Errors;

        await _messagesRepository.UpdateAsync(message, cancellationToken);

        // Linear conversation: editing a user message invalidates everything after it.
        if (message.MessageRole == MessageRole.User)
        {
            var all = await _messagesRepository.ListByConversationIdAsync(
                message.ConversationId,
                cancellationToken);

            foreach (var later in all.Where(m =>
                         m.Id != message.Id &&
                         m.CreatedAt >= message.CreatedAt))
            {
                // Prefer CreatedAt order; if same timestamp, only remove messages that appear after in list.
                if (later.CreatedAt > message.CreatedAt ||
                    (later.CreatedAt == message.CreatedAt &&
                     all.FindIndex(m => m.Id == later.Id) > all.FindIndex(m => m.Id == message.Id)))
                {
                    await _messagesRepository.DeleteAsync(later, cancellationToken);
                }
            }
        }

        await _unitOfWork.CommitChangesAsync();
        return message;
    }

    public async Task<ErrorOr<Success>> DeleteMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        var message = await _messagesRepository.GetByIdAsync(messageId, cancellationToken);
        if (message is null)
            return MessageErrors.NotFound;

        var conversation = await _conversationsRepository.GetByIdAsync(
            message.ConversationId,
            userIdResult.Value,
            cancellationToken);
        if (conversation is null)
            return ConversationErrors.NotFound;

        if (message.Status == MessageStatus.Streaming)
            return MessageErrors.CannotModifyWhileStreaming;

        var all = await _messagesRepository.ListByConversationIdAsync(
            message.ConversationId,
            cancellationToken);

        var index = all.FindIndex(m => m.Id == message.Id);
        if (index < 0)
            return MessageErrors.NotFound;

        // Always delete the target message.
        await _messagesRepository.DeleteAsync(message, cancellationToken);

        // Linear model: deleting a user message also removes the following assistant
        // response (if present) so history stays consistent.
        if (message.MessageRole == MessageRole.User && index + 1 < all.Count)
        {
            var next = all[index + 1];
            if (next.MessageRole == MessageRole.Assistant)
                await _messagesRepository.DeleteAsync(next, cancellationToken);
        }

        await _unitOfWork.CommitChangesAsync();
        return Result.Success;
    }
    private async Task<ErrorOr<Conversation>> GetOwnedConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        var conversation = await _conversationsRepository.GetByIdAsync(
            conversationId,
            userIdResult.Value,
            cancellationToken);
        return conversation is null
            ? ConversationErrors.NotFound
            : conversation;
    }

    private ErrorOr<Guid> GetCurrentUserId()
    {
        return !_userSession.IsAuthenticated || !_userSession.UserId.HasValue || _userSession.UserId == Guid.Empty
            ? AuthenticationErrors.NotAuthenticated
            : _userSession.UserId.Value;
    }

    private static string CreateTitle(string content)
    {
        var title = string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        title = title.Trim().TrimEnd('.', '!', '?', ':', ';');

        if (title.StartsWith("Can you ", StringComparison.OrdinalIgnoreCase))
            title = title["Can you ".Length..];

        if (title.Length > 60)
        {
            title = title[..60];
            var lastSpace = title.LastIndexOf(' ');
            if (lastSpace > 0)
                title = title[..lastSpace];
        }

        return char.ToUpperInvariant(title[0]) + title[1..];
    }
}