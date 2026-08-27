using Microsoft.EntityFrameworkCore;
using RockAI.Application.Common.Interfaces;
using RockAI.Domain.Users;
using RockAI.Infrastructure.Common.Persistence;
using System.Diagnostics;

namespace RockAI.Infrastructure.Users.Persistence;

public class UsersRepository(RockAIDbContext _dbContext) : IUsersRepository
{
    public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.AddAsync(user, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine( $"ACTUAL DATABASE: {_dbContext.Database.GetDbConnection().DataSource}");
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task UpdateAsync(User user)
    {
        _dbContext.Update(user);

        return Task.CompletedTask;
    }
}
