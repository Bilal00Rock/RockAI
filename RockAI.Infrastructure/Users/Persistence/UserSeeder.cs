using RockAI.Domain.Common.Interfaces;
using RockAI.Domain.Users;
using RockAI.Infrastructure.Common.Persistence;

namespace RockAI.Infrastructure.Users.Persistence;

public static class UserSeeder
{
    public static async Task SeedAsync(
        RockAIDbContext dbContext,
        IPasswordHasher passwordHasher)
    {
        if (dbContext.Users.Any())
            return;

        var passwordResult = passwordHasher.HashPassword("Rock123!");

        if (passwordResult.IsError)
            throw new Exception(
                passwordResult.FirstError.Description);

        var user = new User(
            firstName: "Rock",
            lastName: "Admin",
            email: "rock@test.com",
            passwordHash: passwordResult.Value);

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }
}