using IdentityApi.Application.DTOs.Auth;
using IdentityApi.Application.DTOs.Common;
using IdentityApi.Application.DTOs.User;

namespace IdentityApi.Application.Services;

public interface IAuthApplicationService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResponse<UserDto>> RegisterUserAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
