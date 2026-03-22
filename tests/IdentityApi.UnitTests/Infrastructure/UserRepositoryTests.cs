using FluentAssertions;
using IdentityApi.Infrastructure.Data;
using IdentityApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityApi.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for UserRepository using mocked UserManager and RoleManager.
/// UserManager/RoleManager are sealed in the framework, so we mock the store interfaces.
/// </summary>
public sealed class UserRepositoryTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<RoleManager<IdentityRole>> _roleManager;
    private readonly Mock<ILogger<UserRepository>> _logger = new();
    private readonly UserRepository _sut;

    private static readonly ApplicationUser SampleAppUser = new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = "alice@example.com",
        Email = "alice@example.com",
        Name = "Alice Smith"
    };

    public UserRepositoryTests()
    {
        // UserManager requires a store and several optional services — all mocked
        _userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);

        // RoleManager requires a store
        _roleManager = new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(),
            null, null, null, null);

        _sut = new UserRepository(_userManager.Object, _roleManager.Object, _logger.Object);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsMappedDomainUser()
    {
        var userId = Guid.Parse(SampleAppUser.Id);
        _userManager.Setup(m => m.FindByIdAsync(SampleAppUser.Id)).ReturnsAsync(SampleAppUser);
        _userManager.Setup(m => m.GetRolesAsync(SampleAppUser)).ReturnsAsync(new[] { "User" });

        var result = await _sut.GetByIdAsync(userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be("Alice Smith");
        result.Email.Should().Be("alice@example.com");
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ReturnsNull()
    {
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetByEmailAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_ExistingUser_ReturnsMappedDomainUser()
    {
        _userManager.Setup(m => m.FindByEmailAsync("alice@example.com")).ReturnsAsync(SampleAppUser);
        _userManager.Setup(m => m.GetRolesAsync(SampleAppUser)).ReturnsAsync(new[] { "User" });

        var result = await _sut.GetByEmailAsync("alice@example.com");

        result.Should().NotBeNull();
        result!.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentEmail_ReturnsNull()
    {
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetByEmailAsync("ghost@example.com");

        result.Should().BeNull();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SuccessfulCreation_ReturnsTrue()
    {
        var user = IdentityApi.Domain.Entities.User.Create(Guid.NewGuid(), "Alice", "alice@example.com", new[] { "User" });

        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Str0ng!Pass"))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.CreateAsync(user, "Str0ng!Pass");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_IdentityFailure_ReturnsFalse()
    {
        var user = IdentityApi.Domain.Entities.User.Create(Guid.NewGuid(), "Alice", "alice@example.com");

        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate email" }));

        var result = await _sut.CreateAsync(user, "Str0ng!Pass");

        result.Should().BeFalse();
    }

    // ── ValidatePasswordAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ValidatePasswordAsync_CorrectPassword_ReturnsTrue()
    {
        _userManager.Setup(m => m.FindByEmailAsync("alice@example.com")).ReturnsAsync(SampleAppUser);
        _userManager.Setup(m => m.CheckPasswordAsync(SampleAppUser, "Str0ng!Pass")).ReturnsAsync(true);

        var result = await _sut.ValidatePasswordAsync("alice@example.com", "Str0ng!Pass");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WrongPassword_ReturnsFalse()
    {
        _userManager.Setup(m => m.FindByEmailAsync("alice@example.com")).ReturnsAsync(SampleAppUser);
        _userManager.Setup(m => m.CheckPasswordAsync(SampleAppUser, "wrong")).ReturnsAsync(false);

        var result = await _sut.ValidatePasswordAsync("alice@example.com", "wrong");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePasswordAsync_UserNotFound_ReturnsFalse()
    {
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ValidatePasswordAsync("ghost@example.com", "anything");

        result.Should().BeFalse();
    }

    // ── AssignRoleAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRoleAsync_ValidUserAndRole_ReturnsTrue()
    {
        var userId = Guid.Parse(SampleAppUser.Id);
        _userManager.Setup(m => m.FindByIdAsync(SampleAppUser.Id)).ReturnsAsync(SampleAppUser);
        _roleManager.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(true);
        _userManager.Setup(m => m.AddToRoleAsync(SampleAppUser, "Admin")).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(userId, "Admin");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AssignRoleAsync_UserNotFound_ReturnsFalse()
    {
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.AssignRoleAsync(Guid.NewGuid(), "Admin");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssignRoleAsync_RoleDoesNotExist_ReturnsFalse()
    {
        var userId = Guid.Parse(SampleAppUser.Id);
        _userManager.Setup(m => m.FindByIdAsync(SampleAppUser.Id)).ReturnsAsync(SampleAppUser);
        _roleManager.Setup(m => m.RoleExistsAsync("NonExistentRole")).ReturnsAsync(false);

        var result = await _sut.AssignRoleAsync(userId, "NonExistentRole");

        result.Should().BeFalse();
    }

    // ── RoleExistsAsync / EnsureRoleExistsAsync ──────────────────────────────

    [Fact]
    public async Task RoleExistsAsync_ExistingRole_ReturnsTrue()
    {
        _roleManager.Setup(m => m.RoleExistsAsync("Admin")).ReturnsAsync(true);

        var result = await _sut.RoleExistsAsync("Admin");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RoleExistsAsync_NonExistentRole_ReturnsFalse()
    {
        _roleManager.Setup(m => m.RoleExistsAsync("Ghost")).ReturnsAsync(false);

        var result = await _sut.RoleExistsAsync("Ghost");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureRoleExistsAsync_RoleAlreadyExists_DoesNotCreate()
    {
        _roleManager.Setup(m => m.RoleExistsAsync("User")).ReturnsAsync(true);

        await _sut.EnsureRoleExistsAsync("User");

        _roleManager.Verify(m => m.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    [Fact]
    public async Task EnsureRoleExistsAsync_RoleNotExists_CreatesRole()
    {
        _roleManager.Setup(m => m.RoleExistsAsync("NewRole")).ReturnsAsync(false);
        _roleManager.Setup(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == "NewRole")))
            .ReturnsAsync(IdentityResult.Success);

        await _sut.EnsureRoleExistsAsync("NewRole");

        _roleManager.Verify(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == "NewRole")), Times.Once);
    }

    // ── GeneratePasswordResetTokenAsync ──────────────────────────────────────

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_ValidUser_ReturnsUrlEscapedToken()
    {
        var userId = Guid.Parse(SampleAppUser.Id);
        // Token with characters that need URL-encoding
        _userManager.Setup(m => m.FindByIdAsync(SampleAppUser.Id)).ReturnsAsync(SampleAppUser);
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(SampleAppUser))
            .ReturnsAsync("raw+token/with=special&chars");

        var token = await _sut.GeneratePasswordResetTokenAsync(userId);

        // The token should be URL-encoded
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
        token.Should().NotContain("&");
        token.Should().Be(Uri.EscapeDataString("raw+token/with=special&chars"));
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var act = () => _sut.GeneratePasswordResetTokenAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── ResetPasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ReturnsTrue()
    {
        var userId = Guid.Parse(SampleAppUser.Id);
        var escapedToken = Uri.EscapeDataString("raw+token");

        _userManager.Setup(m => m.FindByIdAsync(SampleAppUser.Id)).ReturnsAsync(SampleAppUser);
        _userManager.Setup(m => m.ResetPasswordAsync(SampleAppUser, "raw+token", "NewStr0ng!Pass"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ResetPasswordAsync(userId, escapedToken, "NewStr0ng!Pass");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_UserNotFound_ReturnsFalse()
    {
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ResetPasswordAsync(Guid.NewGuid(), "token", "NewStr0ng!Pass");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ReturnsFalse()
    {
        var userId = Guid.Parse(SampleAppUser.Id);

        _userManager.Setup(m => m.FindByIdAsync(SampleAppUser.Id)).ReturnsAsync(SampleAppUser);
        _userManager.Setup(m => m.ResetPasswordAsync(SampleAppUser, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await _sut.ResetPasswordAsync(userId, "bad-token", "NewStr0ng!Pass");

        result.Should().BeFalse();
    }
}
