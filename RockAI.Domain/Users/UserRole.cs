using Ardalis.SmartEnum;

namespace RockAI.Domain.Users;

public class UserRole : SmartEnum<UserRole>
{
    public static readonly UserRole User = new(nameof(User), 0);
    public static readonly UserRole Admin = new(nameof(Admin), 1);

    public UserRole(string name, int value) : base(name, value)
    {
    }
}