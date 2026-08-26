using ErrorOr;
using RockAI.Domain.Common.Interfaces;

namespace RockAI.Domain.Users;

public class User : Entity
{
    public string FirstName { get; } = null!;
    public string LastName { get; } = null!;
    public string Email { get; } = null!;

    private readonly string _passwordHash = null!;

    private readonly List<UserRole> _roles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _roles.AsReadOnly();

    public User(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        IEnumerable<UserRole>? roles = null,
        Guid? id = null)
            : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException(
                "First name cannot be empty.",
                nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException(
                "Last name cannot be empty.",
                nameof(lastName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "Email cannot be empty.",
                nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "Password hash cannot be empty.",
                nameof(passwordHash));

        FirstName = firstName;
        LastName = lastName;
        Email = email;
        _passwordHash = passwordHash;
        if (roles is not null)
        {
            _roles.AddRange(roles.Distinct());
        }
    }
    private User()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        _passwordHash = string.Empty;
    }

    public bool HasRole(UserRole role)
    {
        return _roles.Contains(role);
    }

    public ErrorOr<Success> AddRole(UserRole role)
    {
        if (HasRole(role))
        {
            return UserErrors.RoleAlreadyAssigned;
        }

        _roles.Add(role);

        return Result.Success;
    }
    public ErrorOr<Success> RemoveRole(UserRole role)
    {
        if (!HasRole(role))
        {
            return UserErrors.RoleNotFound;
        }

        // Don't allow an account to have no role.
        if (_roles.Count == 1)
        {
            return UserErrors.LastRole;
        }

        _roles.Remove(role);

        return Result.Success;
    }

    public bool IsCorrectPasswordHash(string password, IPasswordHasher passwordHasher)
    {
        return passwordHasher.IsCorrectPassword(password, _passwordHash);
    }


}