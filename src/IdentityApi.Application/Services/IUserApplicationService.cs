using IdentityApi.Application.DTOs.Common;
using IdentityApi.Application.DTOs.User;

namespace IdentityApi.Application.Services;

public interface IUserApplicationService
{
    Task<ApiResponse<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiResponse<UserDto>> AssignRoleAsync(AssignRoleRequest request, CancellationToken ct = default);
}
