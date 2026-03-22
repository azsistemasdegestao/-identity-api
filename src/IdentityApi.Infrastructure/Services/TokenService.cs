using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityApi.Domain.Entities;
using IdentityApi.Domain.Services;
using IdentityApi.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace IdentityApi.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration config, ILogger<TokenService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public TokenPair GenerateTokenPair(User user)
    {
        var accessKey = _config["Jwt:KeyAccessToken"] ?? throw new InvalidOperationException("Jwt:KeyAccessToken not configured.");
        var refreshKey = _config["Jwt:KeyRefreshToken"] ?? throw new InvalidOperationException("Jwt:KeyRefreshToken not configured.");
        var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured.");
        var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured.");
        var accessExpireHours = int.Parse(_config["Jwt:ExpiresAccessToken"] ?? "3");
        var refreshExpireHours = int.Parse(_config["Jwt:ExpiresRefreshToken"] ?? "24");

        var accessExpiresAt = DateTime.UtcNow.AddHours(accessExpireHours);
        var refreshExpiresAt = DateTime.UtcNow.AddHours(refreshExpireHours);

        var accessToken = BuildToken(
            user,
            accessKey,
            issuer,
            audience,
            accessExpiresAt,
            includeUserClaims: true);

        var refreshToken = BuildToken(
            user,
            refreshKey,
            issuer,
            audience,
            refreshExpiresAt,
            includeUserClaims: false);

        return new TokenPair(accessToken, refreshToken, accessExpiresAt, refreshExpiresAt);
    }

    public Guid? ValidateRefreshToken(string refreshToken)
    {
        var refreshKey = _config["Jwt:KeyRefreshToken"] ?? throw new InvalidOperationException("Jwt:KeyRefreshToken not configured.");
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(refreshKey);

        try
        {
            var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var sid = principal.FindFirstValue(JwtRegisteredClaimNames.Sid);
            return sid is not null && Guid.TryParse(sid, out var userId) ? userId : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh token validation failed.");
            return null;
        }
    }

    private static string BuildToken(
        User user,
        string signingKey,
        string issuer,
        string audience,
        DateTime expires,
        bool includeUserClaims)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sid, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (includeUserClaims)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.Name));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            foreach (var role in user.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
