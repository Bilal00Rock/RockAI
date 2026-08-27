using ErrorOr;

namespace RockAI.Application.Common.Interfaces;

public interface IAuthenticationService
{
    Task<ErrorOr<AuthenticatedUserResult>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record AuthenticatedUserResult(Guid UserId,
                                                string FirstName,
                                                string LastName,
                                                string Email);