namespace IdentityApi.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    private readonly List<string> _roles = new();
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();

    private User() { }

    public static User Create(Guid id, string name, string email, IEnumerable<string>? roles = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = new User
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id,
            Name = name,
            Email = email.ToLowerInvariant()
        };

        if (roles is not null)
            user._roles.AddRange(roles);

        return user;
    }

    public bool IsInRole(string role) =>
        _roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
