using IdentityApi.Application.DTOs.User;
namespace IdentityApi.Application.DTOs.Auth;
public sealed record LoginResponse(
    string AccessToken, string RefreshToken,
    DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt,
    UserDto User);
