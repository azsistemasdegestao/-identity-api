using System.ComponentModel.DataAnnotations;
namespace IdentityApi.Application.DTOs.Auth;
public sealed record ResetPasswordRequest(
    [Required][EmailAddress] string Email,
    [Required] string Token,
    [Required][MinLength(8)] string NewPassword);
