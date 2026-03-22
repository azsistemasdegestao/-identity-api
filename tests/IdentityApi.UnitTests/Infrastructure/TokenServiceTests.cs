using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using IdentityApi.Domain.Entities;
using IdentityApi.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace IdentityApi.UnitTests.Infrastructure;

public sealed class TokenServiceTests
{
    private readonly TokenService _sut;

    // Minimum 32-char keys required by HMAC-SHA256
    private const string AccessKey = "test-access-key-min-32-chars-xxxx";
    private const string RefreshKey = "test-refresh-key-min-32-chars-xxx";
    private const string Issuer = "https://test.example.com";
    private const string Audience = "https://test.example.com";

    private static readonly User SampleUser = User.Create(
        Guid.NewGuid(), "Alice Smith", "alice@example.com", new[] { "User", "Admin" });

    public TokenServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:KeyAccessToken"] = AccessKey,
            ["Jwt:KeyRefreshToken"] = RefreshKey,
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:ExpiresAccessToken"] = "3",
            ["Jwt:ExpiresRefreshToken"] = "24"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var logger = new Mock<ILogger<TokenService>>().Object;
        _sut = new TokenService(config, logger);
    }

    // ── GenerateTokenPair ────────────────────────────────────────────────────

    [Fact]
    public void GenerateTokenPair_ReturnsNonEmptyTokens()
    {
        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        tokenPair.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokenPair.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateTokenPair_AccessAndRefreshTokensAreDifferent()
    {
        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        tokenPair.AccessToken.Should().NotBe(tokenPair.RefreshToken);
    }

    [Fact]
    public void GenerateTokenPair_AccessTokenExpiresInConfiguredHours()
    {
        var before = DateTime.UtcNow.AddHours(3).AddSeconds(-5);
        var after = DateTime.UtcNow.AddHours(3).AddSeconds(5);

        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        tokenPair.AccessTokenExpiresAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void GenerateTokenPair_RefreshTokenExpiresInConfiguredHours()
    {
        var before = DateTime.UtcNow.AddHours(24).AddSeconds(-5);
        var after = DateTime.UtcNow.AddHours(24).AddSeconds(5);

        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        tokenPair.RefreshTokenExpiresAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void GenerateTokenPair_AccessTokenContainsUserClaims()
    {
        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenPair.AccessToken);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sid && c.Value == SampleUser.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == SampleUser.Name);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == SampleUser.Email);
    }

    [Fact]
    public void GenerateTokenPair_AccessTokenContainsRoleClaims()
    {
        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenPair.AccessToken);

        var roleClaims = jwt.Claims
            .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value)
            .ToList();

        roleClaims.Should().Contain("User");
        roleClaims.Should().Contain("Admin");
    }

    [Fact]
    public void GenerateTokenPair_RefreshTokenOnlyContainsSid()
    {
        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenPair.RefreshToken);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sid);
        jwt.Claims.Should().NotContain(c => c.Type == JwtRegisteredClaimNames.Email);
        jwt.Claims.Should().NotContain(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
    }

    // ── ValidateRefreshToken ─────────────────────────────────────────────────

    [Fact]
    public void ValidateRefreshToken_ValidToken_ReturnsUserId()
    {
        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        var userId = _sut.ValidateRefreshToken(tokenPair.RefreshToken);

        userId.Should().Be(SampleUser.Id);
    }

    [Fact]
    public void ValidateRefreshToken_InvalidToken_ReturnsNull()
    {
        var userId = _sut.ValidateRefreshToken("this.is.not.valid");

        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_AccessTokenUsedAsRefresh_ReturnsNull()
    {
        // Access token is signed with a different key — must not validate as refresh
        var tokenPair = _sut.GenerateTokenPair(SampleUser);

        var userId = _sut.ValidateRefreshToken(tokenPair.AccessToken);

        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_ExpiredToken_ReturnsNull()
    {
        // Build a token that expired 1 hour ago directly via JwtSecurityTokenHandler,
        // bypassing TokenService (which can't create a token with expires < notBefore).
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(RefreshKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, System.IdentityModel.Tokens.Jwt.JwtConstants.TokenType);

        var expiredToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[] { new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sid, SampleUser.Id.ToString()) },
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1),   // expired 1 hour ago
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(expiredToken);

        var userId = _sut.ValidateRefreshToken(tokenString);

        userId.Should().BeNull();
    }
}
