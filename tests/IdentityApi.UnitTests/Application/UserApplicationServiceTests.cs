using FluentAssertions;
using IdentityApi.Application.DTOs.User;
using IdentityApi.Domain.Entities;
using IdentityApi.Domain.Repositories;
using IdentityApi.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityApi.UnitTests.Application;

public sealed class UserApplicationServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ILogger<UserApplicationService>> _logger = new();
    private readonly UserApplicationService _sut;

    private static readonly User SampleUser = User.Create(
        Guid.NewGuid(), "Alice Smith", "alice@example.com", new[] { "User" });

    public UserApplicationServiceTests()
    {
        _sut = new UserApplicationService(_userRepo.Object, _logger.Object);
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsOkWithDto()
    {
        _userRepo.Setup(r => r.GetByIdAsync(SampleUser.Id, default)).ReturnsAsync(SampleUser);

        var result = await _sut.GetByIdAsync(SampleUser.Id);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(SampleUser.Id);
        result.Data.Name.Should().Be("Alice Smith");
        result.Data.Email.Should().Be("alice@example.com");
        result.Data.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ReturnsFail()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((User?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    // ── AssignRole ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRoleAsync_ValidUserAndRole_ReturnsOkWithUpdatedUser()
    {
        var updatedUser = User.Create(SampleUser.Id, SampleUser.Name, SampleUser.Email, new[] { "User", "Admin" });

        _userRepo.Setup(r => r.EnsureRoleExistsAsync("Admin", default)).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.AssignRoleAsync(SampleUser.Id, "Admin", default)).ReturnsAsync(true);
        _userRepo.Setup(r => r.GetByIdAsync(SampleUser.Id, default)).ReturnsAsync(updatedUser);

        var result = await _sut.AssignRoleAsync(new AssignRoleRequest(SampleUser.Id, "Admin"));

        result.Success.Should().BeTrue();
        result.Data!.Roles.Should().Contain("Admin");
        result.Data.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task AssignRoleAsync_AssignFails_ReturnsFail()
    {
        _userRepo.Setup(r => r.EnsureRoleExistsAsync(It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.AssignRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), default)).ReturnsAsync(false);

        var result = await _sut.AssignRoleAsync(new AssignRoleRequest(Guid.NewGuid(), "Admin"));

        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AssignRoleAsync_EnsuresRoleExistsBeforeAssigning()
    {
        _userRepo.Setup(r => r.EnsureRoleExistsAsync("NewRole", default)).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.AssignRoleAsync(SampleUser.Id, "NewRole", default)).ReturnsAsync(true);
        _userRepo.Setup(r => r.GetByIdAsync(SampleUser.Id, default)).ReturnsAsync(SampleUser);

        await _sut.AssignRoleAsync(new AssignRoleRequest(SampleUser.Id, "NewRole"));

        _userRepo.Verify(r => r.EnsureRoleExistsAsync("NewRole", default), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_UserNotFoundAfterAssignment_ReturnsFail()
    {
        _userRepo.Setup(r => r.EnsureRoleExistsAsync(It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.AssignRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((User?)null);

        var result = await _sut.AssignRoleAsync(new AssignRoleRequest(Guid.NewGuid(), "Admin"));

        result.Success.Should().BeFalse();
    }
}
