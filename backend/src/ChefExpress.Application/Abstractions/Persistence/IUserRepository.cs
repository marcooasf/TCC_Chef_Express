using ChefExpress.Domain.Users;

namespace ChefExpress.Application.Abstractions.Persistence;

public interface IUserRepository
{
    /// <param name="normalizedEmail">Already normalized with <see cref="User.NormalizeEmail"/>.</param>
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <param name="normalizedEmail">Already normalized with <see cref="User.NormalizeEmail"/>.</param>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(User user);
}
