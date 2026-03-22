using System.ComponentModel.DataAnnotations;
namespace IdentityApi.Application.DTOs.Auth;
public sealed record RegisterUserRequest(
    [Required][MaxLength(150)] string Name,
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    string? Role = null);
