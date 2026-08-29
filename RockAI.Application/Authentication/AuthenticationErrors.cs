using ErrorOr;

namespace RockAI.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly Error EmailRequired = Error.Validation(
        code: "Auth.EmailRequired",
        description: "Email is required.");

    public static readonly Error PasswordRequired = Error.Validation(
        code: "Auth.PasswordRequired",
        description: "Password is required.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        code: "Auth.InvalidCredentials",
        description: "Invalid email or password.");

    public static readonly Error NotAuthenticated = Error.Unauthorized(
        code: "Auth.NotAuthenticated",
        description: "The current user is not authenticated.");
}
