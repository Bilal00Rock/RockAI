using ErrorOr;
using System;
using System.Collections.Generic;
using System.Text;

namespace RockAI.Domain.Users
{
    public static class UserErrors
    {
        public static readonly Error ConversationWithIdNotAssigned = Error.Validation(
                code: "User.NotConversationWithId",
                description: "Conversation with this ID is not assigned to this user.");
        public static readonly Error RoleNotFound = Error.NotFound(
                code: "User.RoleNotFound",
                description: $"User does not have the required role.");

        public static readonly Error LastRole = Error.Validation(
                code: "User.LastRole",
                description: "A user must have at least one role.");

        public static readonly Error RoleAlreadyAssigned = Error.Conflict(
                code: "User.RoleAlreadyAssigned",
                description: $"User already has role."); 

    }
}
