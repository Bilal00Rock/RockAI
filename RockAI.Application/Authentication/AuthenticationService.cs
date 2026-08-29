using ErrorOr;
using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Common.Interfaces;

namespace RockAI.Application.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUsersRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationService(
        IUsersRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ErrorOr<AuthenticatedUserResult>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthenticationErrors.EmailRequired;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return AuthenticationErrors.PasswordRequired;
        }

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null)
        {
            return AuthenticationErrors.InvalidCredentials;
        }

        if (!user.IsCorrectPasswordHash(password, _passwordHasher))
        {
            return AuthenticationErrors.InvalidCredentials;
        }

        return new AuthenticatedUserResult(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email);
    }
}