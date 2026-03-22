using System.ComponentModel.DataAnnotations;
namespace IdentityApi.Application.DTOs.Auth;
public sealed record RefreshTokenRequest([Required] string RefreshToken);
