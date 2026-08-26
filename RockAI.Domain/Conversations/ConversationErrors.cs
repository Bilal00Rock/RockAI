using ErrorOr;

namespace RockAI.Domain.Conversations;

public static class ConversationErrors
{
    public static readonly Error CannotUpdateCompletedConversation = Error.Validation(
        code: "Conversation.CannotUpdateCompletedConversation",
        description: "Cannot update a completed conversation. Mark it as incomplete first if you need to modify it.");
    
    // You could also add:
    public static readonly Error InvalidTitle = Error.Validation(
        code: "Conversation.InvalidTitle",
        description: "Conversation title cannot be empty or exceed maximum length.");
    
    public static readonly Error InvalidConversationType = Error.Validation(
        code: "Conversation.InvalidConversationType",
        description: "The specified conversation type is invalid.");

}