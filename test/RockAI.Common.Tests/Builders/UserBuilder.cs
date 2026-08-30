using RockAI.Domain.Users;

namespace RockAI.Common.Tests.Builders;

public sealed class UserBuilder
{
    private string _firstName = "Ada";
    private string _lastName = "Lovelace";
    private string _email = "ada@example.com";
    private string _passwordHash = "hashed-password";
    private IEnumerable<UserRole>? _roles;
    private Guid? _id;
    private DateTime? _createdAt;
    private Guid? _createdBy;

    public UserBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithRoles(params UserRole[] roles)
    {
        _roles = roles;
        return this;
    }

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public UserBuilder CreatedBy(Guid createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public User Build() => new(
        _firstName,
        _lastName,
        _email,
        _passwordHash,
        _roles,
        _id,
        _createdAt,
        _createdBy);
}
