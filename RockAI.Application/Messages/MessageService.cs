using ErrorOr;
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

    public async Task<ErrorOr<Message>> SendMessageAsync(
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

        if (existingMessages.Count == 0)
        {
            var title = CreateTitle(content);
            var updateResult = conversationResult.Value.UpdateConversation(
                title,
                conversationResult.Value.ConversationType,
                conversationResult.Value.IsCompleted);

            if (updateResult.IsError)
                return updateResult.Errors;

            await _conversationsRepository.UpdateAsync(
                conversationResult.Value,
                cancellationToken);
        }

        await _unitOfWork.CommitChangesAsync();
        return message;
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
            return Error.NotFound("Message.NotFound", "Message was not found.");

        var conversation = await _conversationsRepository.GetByIdAsync(
            message.ConversationId,
            userIdResult.Value,
            cancellationToken);
        if (conversation is null)
            return Error.NotFound("Conversation.NotFound", "Conversation was not found.");

        var updateResult = message.UpdateMessage(content, messageRole, status);
        if (updateResult.IsError)
            return updateResult.Errors;

        await _messagesRepository.UpdateAsync(message, cancellationToken);
        await _unitOfWork.CommitChangesAsync();
        return message;
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
            ? Error.NotFound("Conversation.NotFound", "Conversation was not found.")
            : conversation;
    }

    private ErrorOr<Guid> GetCurrentUserId()
    {
        return !_userSession.IsAuthenticated || !_userSession.UserId.HasValue || _userSession.UserId == Guid.Empty
            ? Error.Unauthorized("Auth.NotAuthenticated", "The current user is not authenticated.")
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