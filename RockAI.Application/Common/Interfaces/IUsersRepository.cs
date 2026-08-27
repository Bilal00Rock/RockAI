using RockAI.Domain.Users;

namespace RockAI.Application.Common.Interfaces;

public interface IUsersRepository
{
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}