using IdentityApi.Application.DTOs.Auth;
using IdentityApi.Application.DTOs.Common;
using IdentityApi.Application.DTOs.User;
using IdentityApi.Application.Services;
using IdentityApi.Domain.Entities;
using IdentityApi.Domain.Repositories;
using IdentityApi.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityApi.Infrastructure.Services;

public sealed class AuthApplicationService : IAuthApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthApplicationService> _logger;

    public AuthApplicationService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IEmailService emailService,
        IConfiguration config,
        ILogger<AuthApplicationService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Login attempt for {Email}.", MaskEmail(request.Email));

        var isValid = await _userRepository.ValidatePasswordAsync(request.Email, request.Password, ct);
        if (!isValid)
        {
            _logger.LogWarning("Invalid credentials for {Email}.", MaskEmail(request.Email));
            return ApiResponse<LoginResponse>.Fail("Invalid email or password.");
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null)
            return ApiResponse<LoginResponse>.Fail("User not found.");

        var tokenPair = _tokenService.GenerateTokenPair(user);
        var response = BuildLoginResponse(user, tokenPair);

        _logger.LogInformation("User {Email} logged in successfully.", MaskEmail(request.Email));
        return ApiResponse<LoginResponse>.Ok(response);
    }

    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var userId = _tokenService.ValidateRefreshToken(request.RefreshToken);
        if (userId is null)
        {
            _logger.LogWarning("Invalid or expired refresh token.");
            return ApiResponse<LoginResponse>.Fail("Invalid or expired refresh token.");
        }

        var user = await _userRepository.GetByIdAsync(userId.Value, ct);
        if (user is null)
            return ApiResponse<LoginResponse>.Fail("User not found.");

        var tokenPair = _tokenService.GenerateTokenPair(user);
        var response = BuildLoginResponse(user, tokenPair);

        _logger.LogInformation("Tokens refreshed for user {UserId}.", userId);
        return ApiResponse<LoginResponse>.Ok(response);
    }

    public async Task<ApiResponse<UserDto>> RegisterUserAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Registration attempt for {Email}.", MaskEmail(request.Email));

        var existing = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
        {
            _logger.LogWarning("Email already registered: {Email}.", MaskEmail(request.Email));
            return ApiResponse<UserDto>.Fail("Email is already registered.");
        }

        var role = request.Role ?? "User";
        await _userRepository.EnsureRoleExistsAsync(role, ct);

        var user = User.Create(Guid.NewGuid(), request.Name, request.Email, new[] { role });
        var created = await _userRepository.CreateAsync(user, request.Password, ct);
        if (!created)
            return ApiResponse<UserDto>.Fail("Failed to create user.");

        _ = Task.Run(() => _emailService.SendWelcomeEmailAsync(user.Email, user.Name), ct);

        var dto = MapToDto(user);
        _logger.LogInformation("User {Email} registered successfully.", MaskEmail(request.Email));
        return ApiResponse<UserDto>.Ok(dto, "User registered successfully.");
    }

    public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Forgot password requested for {Email}.", MaskEmail(request.Email));

        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is not null)
        {
            try
            {
                var token = await _userRepository.GeneratePasswordResetTokenAsync(user.Id, ct);
                var baseUrl = _config["App:PasswordResetUrl"] ?? "https://app.yourdomain.com/reset-password";
                var resetUrl = $"{baseUrl}?email={Uri.EscapeDataString(user.Email)}&token={token}";

                _ = Task.Run(() => _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetUrl), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token for {Email}.", MaskEmail(request.Email));
            }
        }

        // Always return success to prevent user enumeration
        return ApiResponse.Ok("If the email is registered, a reset link has been sent.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Password reset attempt for {Email}.", MaskEmail(request.Email));

        var user = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null)
            return ApiResponse.Fail("Invalid request.");

        var success = await _userRepository.ResetPasswordAsync(user.Id, request.Token, request.NewPassword, ct);
        if (!success)
        {
            _logger.LogWarning("Password reset failed for {Email}.", MaskEmail(request.Email));
            return ApiResponse.Fail("Invalid or expired token.");
        }

        _logger.LogInformation("Password reset successfully for {Email}.", MaskEmail(request.Email));
        return ApiResponse.Ok("Password has been reset successfully.");
    }

    private static LoginResponse BuildLoginResponse(User user, Domain.ValueObjects.TokenPair tokenPair)
        => new(
            tokenPair.AccessToken,
            tokenPair.RefreshToken,
            tokenPair.AccessTokenExpiresAt,
            tokenPair.RefreshTokenExpiresAt,
            MapToDto(user));

    private static UserDto MapToDto(User user)
        => new(user.Id, user.Name, user.Email, user.Roles);

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return "***";
        return email[0] + "***" + email[atIndex..];
    }
}
