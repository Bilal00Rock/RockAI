using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Conversations;

namespace RockAI.Application.Conversations;

public sealed class ConversationService : IConversationService
{
    private readonly IConversationsRepository _conversationsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;

    public ConversationService(
        IConversationsRepository conversationsRepository,
        IUnitOfWork unitOfWork,
        IUserSession userSession)
    {
        _conversationsRepository = conversationsRepository;
        _unitOfWork = unitOfWork;
        _userSession = userSession;
    }

    public async Task<ErrorOr<Conversation>> CreateConversationAsync(
        string title,
        ConversationType? conversationType = null,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        if (string.IsNullOrWhiteSpace(title))
            return ConversationErrors.InvalidTitle;

        Conversation conversation;
        try
        {
            conversation = new Conversation(
                conversationType ?? ConversationType.General,
                title,
                userIdResult.Value);
        }
        catch (ArgumentException)
        {
            return ConversationErrors.InvalidTitle;
        }

        await _conversationsRepository.AddConversationAsync(conversation, cancellationToken);
        await _unitOfWork.CommitChangesAsync();
        return conversation;
    }

    public async Task<ErrorOr<List<Conversation>>> GetUserConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        return await _conversationsRepository.ListByUserIdAsync(userIdResult.Value, cancellationToken);
    }

    public async Task<ErrorOr<Conversation>> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        return await GetOwnedConversationAsync(conversationId, userIdResult.Value, cancellationToken);
    }

    public async Task<ErrorOr<Conversation>> UpdateConversationAsync(
        Guid conversationId,
        string title,
        ConversationType conversationType,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        var conversationResult = await GetOwnedConversationAsync(conversationId, userIdResult.Value, cancellationToken);
        if (conversationResult.IsError)
            return conversationResult.Errors;

        var updateResult = conversationResult.Value.UpdateConversation(title, conversationType, isCompleted);
        if (updateResult.IsError)
            return updateResult.Errors;

        await _conversationsRepository.UpdateAsync(conversationResult.Value, cancellationToken);
        await _unitOfWork.CommitChangesAsync();
        return conversationResult.Value;
    }

    public async Task<ErrorOr<Conversation>> CompleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsError)
            return userIdResult.Errors;

        var conversationResult = await GetOwnedConversationAsync(conversationId, userIdResult.Value, cancellationToken);
        if (conversationResult.IsError)
            return conversationResult.Errors;

        conversationResult.Value.MarkAsCompleted();
        await _conversationsRepository.UpdateAsync(conversationResult.Value, cancellationToken);
        await _unitOfWork.CommitChangesAsync();
        return conversationResult.Value;
    }

    private async Task<ErrorOr<Conversation>> GetOwnedConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationsRepository.GetByIdAsync(conversationId, userId, cancellationToken);
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
}