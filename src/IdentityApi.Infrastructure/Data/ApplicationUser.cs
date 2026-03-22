using Microsoft.AspNetCore.Identity;
namespace IdentityApi.Infrastructure.Data;
public sealed class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
}
