using RockAI.Application.Authentication;

namespace RockAI.Application.Common.Interfaces;

public interface IUserSession
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }
    string? Email { get; }
    string? FirstName { get; }
    string? LastName { get; }
    string? FullName { get; }

    Task LoadAsync();
    Task SetAsync(AuthenticatedUserResult user);
    Task ClearAsync();
}