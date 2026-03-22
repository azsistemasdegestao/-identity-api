using System.ComponentModel.DataAnnotations;
namespace IdentityApi.Application.DTOs.User;
public sealed record AssignRoleRequest([Required] Guid UserId, [Required] string Role);
