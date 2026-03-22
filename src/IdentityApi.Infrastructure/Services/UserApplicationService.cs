using IdentityApi.Application.DTOs.Common;
using IdentityApi.Application.DTOs.User;
using IdentityApi.Application.Services;
using IdentityApi.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IdentityApi.Infrastructure.Services;

public sealed class UserApplicationService : IUserApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserApplicationService> _logger;

    public UserApplicationService(IUserRepository userRepository, ILogger<UserApplicationService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null)
        {
            _logger.LogWarning("User {Id} not found.", id);
            return ApiResponse<UserDto>.Fail("User not found.");
        }

        return ApiResponse<UserDto>.Ok(new UserDto(user.Id, user.Name, user.Email, user.Roles));
    }

    public async Task<ApiResponse<UserDto>> AssignRoleAsync(AssignRoleRequest request, CancellationToken ct = default)
    {
        await _userRepository.EnsureRoleExistsAsync(request.Role, ct);

        var success = await _userRepository.AssignRoleAsync(request.UserId, request.Role, ct);
        if (!success)
        {
            _logger.LogWarning("Failed to assign role {Role} to user {UserId}.", request.Role, request.UserId);
            return ApiResponse<UserDto>.Fail("Failed to assign role. User not found or role assignment failed.");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return ApiResponse<UserDto>.Fail("User not found after role assignment.");

        _logger.LogInformation("Role {Role} assigned to user {UserId}.", request.Role, request.UserId);
        return ApiResponse<UserDto>.Ok(new UserDto(user.Id, user.Name, user.Email, user.Roles));
    }
}
