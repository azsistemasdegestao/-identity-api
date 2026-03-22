using FluentAssertions;
using IdentityApi.Domain.Entities;

namespace IdentityApi.UnitTests.Domain;

public sealed class UserEntityTests
{
    [Fact]
    public void Create_WithValidData_ReturnsUser()
    {
        var id = Guid.NewGuid();
        var user = User.Create(id, "Alice Smith", "alice@example.com");

        user.Id.Should().Be(id);
        user.Name.Should().Be("Alice Smith");
        user.Email.Should().Be("alice@example.com");
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyGuid_GeneratesNewId()
    {
        var user = User.Create(Guid.Empty, "Alice", "alice@example.com");

        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_NormalizesEmailToLowercase()
    {
        var user = User.Create(Guid.NewGuid(), "Alice", "ALICE@EXAMPLE.COM");

        user.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void Create_WithRoles_AssignsRoles()
    {
        var user = User.Create(Guid.NewGuid(), "Alice", "alice@example.com", new[] { "Admin", "User" });

        user.Roles.Should().Contain("Admin");
        user.Roles.Should().Contain("User");
        user.Roles.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsArgumentException(string name)
    {
        var act = () => User.Create(Guid.NewGuid(), name, "alice@example.com");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankEmail_ThrowsArgumentException(string email)
    {
        var act = () => User.Create(Guid.NewGuid(), "Alice", email);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsInRole_WhenUserHasRole_ReturnsTrue()
    {
        var user = User.Create(Guid.NewGuid(), "Alice", "alice@example.com", new[] { "Admin" });

        user.IsInRole("Admin").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_IsCaseInsensitive()
    {
        var user = User.Create(Guid.NewGuid(), "Alice", "alice@example.com", new[] { "Admin" });

        user.IsInRole("admin").Should().BeTrue();
        user.IsInRole("ADMIN").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_WhenUserDoesNotHaveRole_ReturnsFalse()
    {
        var user = User.Create(Guid.NewGuid(), "Alice", "alice@example.com", new[] { "User" });

        user.IsInRole("Admin").Should().BeFalse();
    }

    [Fact]
    public void Roles_IsReadOnly_CannotBeModifiedExternally()
    {
        var user = User.Create(Guid.NewGuid(), "Alice", "alice@example.com");

        // The returned collection is read-only — casting should still not mutate the entity
        var roles = user.Roles;
        var act = () => ((System.Collections.Generic.ICollection<string>)roles).Add("Admin");

        act.Should().Throw<NotSupportedException>();
    }
}
