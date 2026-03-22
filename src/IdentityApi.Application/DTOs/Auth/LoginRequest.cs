using System.ComponentModel.DataAnnotations;
namespace IdentityApi.Application.DTOs.Auth;
public sealed record LoginRequest([Required][EmailAddress] string Email, [Required] string Password);
