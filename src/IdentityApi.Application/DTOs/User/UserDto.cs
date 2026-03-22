namespace IdentityApi.Application.DTOs.User;
public sealed record UserDto(Guid Id, string Name, string Email, IReadOnlyCollection<string> Roles);
