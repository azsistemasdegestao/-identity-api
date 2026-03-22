using IdentityApi.Domain.Entities;
using IdentityApi.Domain.Repositories;
using IdentityApi.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace IdentityApi.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserRepository> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(id.ToString());
        return appUser is null ? null : await MapToUserAsync(appUser);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email);
        return appUser is null ? null : await MapToUserAsync(appUser);
    }

    public async Task<bool> CreateAsync(User user, string password, CancellationToken ct = default)
    {
        var appUser = new ApplicationUser
        {
            Id = user.Id.ToString(),
            UserName = user.Email,
            Email = user.Email,
            Name = user.Name,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(appUser, password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        foreach (var role in user.Roles)
        {
            await _userManager.AddToRoleAsync(appUser, role);
        }

        return true;
    }

    public async Task<bool> ValidatePasswordAsync(string email, string password, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email);
        if (appUser is null) return false;
        return await _userManager.CheckPasswordAsync(appUser, password);
    }

    public async Task<bool> AssignRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        if (appUser is null) return false;

        if (!await _roleManager.RoleExistsAsync(role))
            return false;

        var result = await _userManager.AddToRoleAsync(appUser, role);
        return result.Succeeded;
    }

    public async Task<bool> RoleExistsAsync(string role, CancellationToken ct = default)
        => await _roleManager.RoleExistsAsync(role);

    public async Task EnsureRoleExistsAsync(string role, CancellationToken ct = default)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(appUser);
        return Uri.EscapeDataString(token);
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        if (appUser is null) return false;

        var unescapedToken = Uri.UnescapeDataString(token);
        var result = await _userManager.ResetPasswordAsync(appUser, unescapedToken, newPassword);
        return result.Succeeded;
    }

    private async Task<User> MapToUserAsync(ApplicationUser appUser)
    {
        var roles = await _userManager.GetRolesAsync(appUser);
        return User.Create(
            Guid.Parse(appUser.Id),
            appUser.Name,
            appUser.Email ?? string.Empty,
            roles);
    }
}
