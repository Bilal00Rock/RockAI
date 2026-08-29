using System.Text.RegularExpressions;

using ErrorOr;
using RockAI.Domain.Users;
using RockAI.Domain.Common.Interfaces;

namespace RockAI.Infrastructure.Authentication.PasswordHasher;

public partial class PasswordHasher : IPasswordHasher
{
    private static readonly Regex PasswordRegex = StrongPasswordRegex;

    public ErrorOr<string> HashPassword(string password)
    {
        return !PasswordRegex.IsMatch(password)
            ? UserErrors.PasswordTooWeak
            : BCrypt.Net.BCrypt.EnhancedHashPassword(password);
    }

    public bool IsCorrectPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
    }

    // https://stackoverflow.com/a/34715674/10091553
    // [GeneratedRegex("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", RegexOptions.Compiled)]
    // private static partial Regex StrongPasswordRegex();
    private static Regex StrongPasswordRegex => new Regex(
        "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", 
        RegexOptions.Compiled);
}
