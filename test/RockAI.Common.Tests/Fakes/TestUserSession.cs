using RockAI.Application.Authentication;
using RockAI.Application.Common.Interfaces;

namespace RockAI.Common.Tests.Fakes;

public sealed class TestUserSession : IUserSession
{
    public bool IsAuthenticated { get; private set; }
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? FullName => FirstName is null || LastName is null
        ? null
        : $"{FirstName} {LastName}";

    public Task LoadAsync() => Task.CompletedTask;

    public Task SetAsync(AuthenticatedUserResult user)
    {
        IsAuthenticated = true;
        UserId = user.UserId;
        Email = user.Email;
        FirstName = user.FirstName;
        LastName = user.LastName;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        IsAuthenticated = false;
        UserId = null;
        Email = null;
        FirstName = null;
        LastName = null;
        return Task.CompletedTask;
    }

    public TestUserSession AuthenticatedAs(Guid userId)
    {
        IsAuthenticated = true;
        UserId = userId;
        return this;
    }
}
