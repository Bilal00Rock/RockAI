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
            return Error.Validation(
                code: "Auth.EmailRequired",
                description: "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return Error.Validation(
                code: "Auth.PasswordRequired",
                description: "Password is required.");
        }

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null)
        {
            return Error.Unauthorized(
                code: "Auth.InvalidCredentials",
                description: "Invalid email or password.");
        }

        if (!user.IsCorrectPasswordHash(password, _passwordHasher))
        {
            return Error.Unauthorized(
                code: "Auth.InvalidCredentials",
                description: "Invalid email or password.");
        }

        return new AuthenticatedUserResult(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email);
    }
}