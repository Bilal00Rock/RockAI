using RockAI.Application.Authentication;
using RockAI.Application.Common.Interfaces;

namespace RockAI.App.Services.Authentication;

public class UserSession : IUserSession
{
    private AuthenticatedUserResult? _user;

    public bool IsAuthenticated => _user is not null;

    public Guid? UserId => _user?.UserId;

    public string? Email => _user?.Email;

    public string? FirstName => _user?.FirstName;

    public string? LastName => _user?.LastName;

    public string? FullName =>
        _user is null
            ? null
            : $"{_user.FirstName} {_user.LastName}".Trim();

    public async Task LoadAsync()
    {
        var userId = await SecureStorage.Default.GetAsync(UserIdKey);
        var email = await SecureStorage.Default.GetAsync(EmailKey);

        if (!Guid.TryParse(userId, out var parsedUserId) ||
            parsedUserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(email))
        {
            _user = null;
            return;
        }

        _user = new AuthenticatedUserResult(
            parsedUserId,
            await SecureStorage.Default.GetAsync(FirstNameKey) ?? string.Empty,
            await SecureStorage.Default.GetAsync(LastNameKey) ?? string.Empty,
            email);
    }

    public async Task SetAsync(AuthenticatedUserResult user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await SecureStorage.Default.SetAsync(UserIdKey, user.UserId.ToString());
        await SecureStorage.Default.SetAsync(EmailKey, user.Email);
        await SecureStorage.Default.SetAsync(FirstNameKey, user.FirstName);
        await SecureStorage.Default.SetAsync(LastNameKey, user.LastName);
        _user = user;
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(FirstNameKey);
        SecureStorage.Default.Remove(LastNameKey);
        _user = null;
        return Task.CompletedTask;
    }

    private const string UserIdKey = "rockai.session.user-id";
    private const string EmailKey = "rockai.session.email";
    private const string FirstNameKey = "rockai.session.first-name";
    private const string LastNameKey = "rockai.session.last-name";
}