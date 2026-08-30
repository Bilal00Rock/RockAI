using ErrorOr;

namespace RockAI.Domain.Messages;

public static class MessageErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Message.NotFound",
        description: "Message was not found.");

    public static readonly Error CannotUpdateCompletedMessage = Error.Validation(
        code: "Message.CannotUpdateCompletedMessage",
        description: "Cannot update a completed message. Mark it as incomplete first if you need to modify it.");
    
    // You could also add:
    public static readonly Error InvalidTitle = Error.Validation(
        code: "Message.InvalidTitle",
        description: "Message title cannot be empty or exceed maximum length.");
    
    public static readonly Error InvalidMessageType = Error.Validation(
        code: "Message.InvalidMessageType",
        description: "The specified message type is invalid.");

    public static readonly Error InvalidContent = Error.Validation(
        code: "Message.InvalidContent",
        description: "Message content cannot be empty or exceed maximum length.");

    public static readonly Error CannotModifyWhileStreaming = Error.Validation(
        code: "Message.CannotModifyWhileStreaming",
        description: "Cannot edit or delete a message while it is still streaming.");
}