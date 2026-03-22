using System.ComponentModel.DataAnnotations;
namespace IdentityApi.Application.DTOs.Auth;
public sealed record ForgotPasswordRequest([Required][EmailAddress] string Email);
