using FluentAssertions;
using IdentityApi.Application.DTOs.Auth;
using IdentityApi.Domain.Entities;
using IdentityApi.Domain.Repositories;
using IdentityApi.Domain.Services;
using IdentityApi.Domain.ValueObjects;
using IdentityApi.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityApi.UnitTests.Application;

public sealed class AuthApplicationServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IConfiguration> _config = new();
    private readonly Mock<ILogger<AuthApplicationService>> _logger = new();
    private readonly AuthApplicationService _sut;

    private static readonly User SampleUser = User.Create(
        Guid.NewGuid(), "Alice Smith", "alice@example.com", new[] { "User" });

    private static readonly TokenPair SampleTokenPair = new(
        "access.token", "refresh.token",
        DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(24));

    public AuthApplicationServiceTests()
    {
        _config.Setup(c => c["App:PasswordResetUrl"]).Returns("https://app.example.com/reset-password");

        _sut = new AuthApplicationService(
            _userRepo.Object,
            _tokenService.Object,
            _emailService.Object,
            _config.Object,
            _logger.Object);
    }

    // ── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsOkWithTokens()
    {
        _userRepo.Setup(r => r.ValidatePasswordAsync("alice@example.com", "password123", default)).ReturnsAsync(true);
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com", default)).ReturnsAsync(SampleUser);
        _tokenService.Setup(t => t.GenerateTokenPair(SampleUser)).Returns(SampleTokenPair);

        var result = await _sut.LoginAsync(new LoginRequest("alice@example.com", "password123"));

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("access.token");
        result.Data.User.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFail()
    {
        _userRepo.Setup(r => r.ValidatePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(false);

        var result = await _sut.LoginAsync(new LoginRequest("alice@example.com", "wrong"));

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoginAsync_UserNotFoundAfterValidation_ReturnsFail()
    {
        _userRepo.Setup(r => r.ValidatePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest("alice@example.com", "password123"));

        result.Success.Should().BeFalse();
    }

    // ── RefreshToken ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsOkWithNewTokens()
    {
        var userId = SampleUser.Id;
        _tokenService.Setup(t => t.ValidateRefreshToken("valid.refresh.token")).Returns(userId);
        _userRepo.Setup(r => r.GetByIdAsync(userId, default)).ReturnsAsync(SampleUser);
        _tokenService.Setup(t => t.GenerateTokenPair(SampleUser)).Returns(SampleTokenPair);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest("valid.refresh.token"));

        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access.token");
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsFail()
    {
        _tokenService.Setup(t => t.ValidateRefreshToken(It.IsAny<string>())).Returns((Guid?)null);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest("invalid.token"));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_UserNotFound_ReturnsFail()
    {
        _tokenService.Setup(t => t.ValidateRefreshToken(It.IsAny<string>())).Returns(Guid.NewGuid());
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((User?)null);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest("token"));

        result.Success.Should().BeFalse();
    }

    // ── RegisterUser ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterUserAsync_NewEmail_ReturnsOkWithUser()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.EnsureRoleExistsAsync("User", default)).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), "Str0ng!Pass", default)).ReturnsAsync(true);
        _emailService.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var request = new RegisterUserRequest("Alice Smith", "alice@example.com", "Str0ng!Pass");
        var result = await _sut.RegisterUserAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be("alice@example.com");
        result.Data.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task RegisterUserAsync_ExistingEmail_ReturnsFail()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com", default)).ReturnsAsync(SampleUser);

        var request = new RegisterUserRequest("Alice", "alice@example.com", "Str0ng!Pass");
        var result = await _sut.RegisterUserAsync(request);

        result.Success.Should().BeFalse();
        _userRepo.Verify(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task RegisterUserAsync_RepositoryFailure_ReturnsFail()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.EnsureRoleExistsAsync(It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<string>(), default)).ReturnsAsync(false);

        var result = await _sut.RegisterUserAsync(new RegisterUserRequest("Alice", "alice@example.com", "Str0ng!Pass"));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterUserAsync_CustomRole_AssignsCustomRole()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.EnsureRoleExistsAsync("Admin", default)).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<string>(), default)).ReturnsAsync(true);
        _emailService.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.RegisterUserAsync(new RegisterUserRequest("Alice", "alice@example.com", "Str0ng!Pass", "Admin"));

        result.Success.Should().BeTrue();
        result.Data!.Roles.Should().Contain("Admin");
    }

    // ── ForgotPassword ───────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_ExistingUser_ReturnsOkAndQueuesEmail()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com", default)).ReturnsAsync(SampleUser);
        _userRepo.Setup(r => r.GeneratePasswordResetTokenAsync(SampleUser.Id, default)).ReturnsAsync("reset-token");
        _emailService.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest("alice@example.com"));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPasswordAsync_NonExistentUser_StillReturnsOk()
    {
        // Anti-enumeration: must return 200 even when user doesn't exist
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest("ghost@example.com"));

        result.Success.Should().BeTrue();
        _emailService.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    // ── ResetPassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ReturnsOk()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com", default)).ReturnsAsync(SampleUser);
        _userRepo.Setup(r => r.ResetPasswordAsync(SampleUser.Id, "valid-token", "NewStr0ng!Pass", default)).ReturnsAsync(true);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest("alice@example.com", "valid-token", "NewStr0ng!Pass"));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ReturnsFail()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com", default)).ReturnsAsync(SampleUser);
        _userRepo.Setup(r => r.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(false);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest("alice@example.com", "bad-token", "NewStr0ng!Pass"));

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_UserNotFound_ReturnsFail()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest("ghost@example.com", "token", "NewStr0ng!Pass"));

        result.Success.Should().BeFalse();
    }
}
