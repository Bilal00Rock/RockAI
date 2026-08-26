using RockAI.Domain.Users;

namespace RockAI.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}