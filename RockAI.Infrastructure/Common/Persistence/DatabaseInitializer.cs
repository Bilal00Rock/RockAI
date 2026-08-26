using Microsoft.EntityFrameworkCore;
using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Users;
using System.Threading.Tasks;
using RockAI.Domain.Common.Interfaces;

namespace RockAI.Infrastructure.Common.Persistence;

public class DatabaseInitializer
{
    private readonly IUsersRepository _usersRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RockAIDbContext _dbContext;

    public DatabaseInitializer(IUsersRepository usersRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, RockAIDbContext dbContext)
    {
        _usersRepository = usersRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _dbContext = dbContext;
    }

    public async Task InitializeAsync()
    {
        // Apply migrations if any exist, otherwise create the database.
        await _dbContext.Database.MigrateAsync();

        const string adminEmail = "admin@example.com";
        const string adminPassword = "Password123!"; // change after first run

        if (!await _usersRepository.ExistsByEmailAsync(adminEmail))
        {
            var hashResult = _passwordHasher.HashPassword(adminPassword);
            if (hashResult.IsError)
            {
                // If hashing failed (e.g., password policy), throw so developer can fix
                throw new InvalidOperationException("Failed to create admin password hash: " + string.Join(';', hashResult.FirstError.Description));
            }

            var user = new User(
                firstName: "Administrator",
                lastName: "",
                email: adminEmail,
                passwordHash: hashResult.Value,
                roles: new[] { UserRole.Admin }
            );

            await _usersRepository.AddUserAsync(user);
            await _unitOfWork.CommitChangesAsync();
        }
    }
}
