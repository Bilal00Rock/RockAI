using Ardalis.SmartEnum;

namespace RockAI.Domain.Users;

public class ProfileType(string name, int value) : SmartEnum<ProfileType>(name, value)
{
    public static readonly ProfileType Admin = new(nameof(Admin), 0);
    public static readonly ProfileType NormalUser = new(nameof(NormalUser), 1);
}