using FluentAssertions;
using NSubstitute;
using RockAI.Application.Authentication;
using RockAI.Application.Common.Interfaces;
using RockAI.Common.Tests.Builders;
using RockAI.Domain.Common.Interfaces;
using RockAI.Domain.Users;

namespace RockAI.Application.Tests.Authentication;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsAuthenticatedUser()
    {
        var users = Substitute.For<IUsersRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var user = new UserBuilder().WithEmail("ada@example.com").Build();
        users.GetByEmailAsync("ada@example.com", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user));
        hasher.IsCorrectPassword("password", "hashed-password").Returns(true);
        var service = new AuthenticationService(users, hasher);

        var result = await service.LoginAsync("ada@example.com", "password");

        result.IsError.Should().BeFalse();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ReturnsInvalidCredentials()
    {
        var users = Substitute.For<IUsersRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<RockAI.Domain.Users.User?>(null));
        var service = new AuthenticationService(users, hasher);

        var result = await service.LoginAsync("missing@example.com", "password");

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(AuthenticationErrors.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_WhenEmailIsBlank_DoesNotQueryRepository()
    {
        var users = Substitute.For<IUsersRepository>();
        var hasher = Substitute.For<IPasswordHasher>();
        var service = new AuthenticationService(users, hasher);

        var result = await service.LoginAsync(" ", "password");

        result.FirstError.Should().Be(AuthenticationErrors.EmailRequired);
        await users.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
