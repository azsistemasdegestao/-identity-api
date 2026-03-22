using IdentityApi.Domain.Entities;

namespace IdentityApi.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> CreateAsync(User user, string password, CancellationToken ct = default);
    Task<bool> ValidatePasswordAsync(string email, string password, CancellationToken ct = default);
    Task<bool> AssignRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<bool> RoleExistsAsync(string role, CancellationToken ct = default);
    Task EnsureRoleExistsAsync(string role, CancellationToken ct = default);
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default);
}
