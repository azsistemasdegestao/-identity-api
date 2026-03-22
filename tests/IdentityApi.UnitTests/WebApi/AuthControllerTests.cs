using FluentAssertions;
using IdentityApi.Application.DTOs.Auth;
using IdentityApi.Application.DTOs.Common;
using IdentityApi.Application.DTOs.User;
using IdentityApi.Application.Services;
using IdentityApi.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdentityApi.UnitTests.WebApi;

public sealed class AuthControllerTests
{
    private readonly Mock<IAuthApplicationService> _authService = new();
    private readonly AuthController _sut;

    private static readonly UserDto SampleUserDto = new(
        Guid.NewGuid(), "Alice Smith", "alice@example.com", new[] { "User" });

    private static readonly LoginResponse SampleLoginResponse = new(
        "access.token", "refresh.token",
        DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(24),
        SampleUserDto);

    public AuthControllerTests()
    {
        _sut = new AuthController(_authService.Object);
    }

    // ── POST /register ───────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Success_Returns200WithBody()
    {
        _authService
            .Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(ApiResponse<UserDto>.Ok(SampleUserDto));

        var result = await _sut.Register(new RegisterUserRequest("Alice", "alice@example.com", "Str0ng!Pass"), default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        body.Success.Should().BeTrue();
        body.Data!.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        _authService
            .Setup(s => s.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(ApiResponse<UserDto>.Fail("Email is already registered."));

        var result = await _sut.Register(new RegisterUserRequest("Alice", "alice@example.com", "Str0ng!Pass"), default);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.StatusCode.Should().Be(409);
        var body = conflict.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        body.Success.Should().BeFalse();
    }

    // ── POST /login ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), default))
            .ReturnsAsync(ApiResponse<LoginResponse>.Ok(SampleLoginResponse));

        var result = await _sut.Login(new LoginRequest("alice@example.com", "Str0ng!Pass"), default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
        body.Data!.AccessToken.Should().Be("access.token");
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), default))
            .ReturnsAsync(ApiResponse<LoginResponse>.Fail("Invalid email or password."));

        var result = await _sut.Login(new LoginRequest("alice@example.com", "wrong"), default);

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorized.StatusCode.Should().Be(401);
    }

    // ── POST /refresh-token ──────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_ValidToken_Returns200WithNewTokens()
    {
        _authService
            .Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), default))
            .ReturnsAsync(ApiResponse<LoginResponse>.Ok(SampleLoginResponse));

        var result = await _sut.RefreshToken(new RefreshTokenRequest("valid.refresh.token"), default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_Returns401()
    {
        _authService
            .Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), default))
            .ReturnsAsync(ApiResponse<LoginResponse>.Fail("Invalid or expired refresh token."));

        var result = await _sut.RefreshToken(new RefreshTokenRequest("bad.token"), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ── POST /forgot-password ─────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_AnyEmail_AlwaysReturns200()
    {
        _authService
            .Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>(), default))
            .ReturnsAsync(ApiResponse.Ok("If the email is registered, a reset link has been sent."));

        var result = await _sut.ForgotPassword(new ForgotPasswordRequest("ghost@example.com"), default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    // ── POST /reset-password ─────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_ValidToken_Returns200()
    {
        _authService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>(), default))
            .ReturnsAsync(ApiResponse.Ok("Password has been reset successfully."));

        var result = await _sut.ResetPassword(
            new ResetPasswordRequest("alice@example.com", "valid-token", "NewStr0ng!Pass"), default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        _authService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>(), default))
            .ReturnsAsync(ApiResponse.Fail("Invalid or expired token."));

        var result = await _sut.ResetPassword(
            new ResetPasswordRequest("alice@example.com", "bad-token", "NewStr0ng!Pass"), default);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);
    }
}
