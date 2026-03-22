using FluentAssertions;
using IdentityApi.Domain.ValueObjects;

namespace IdentityApi.UnitTests.Domain;

public sealed class TokenPairTests
{
    [Fact]
    public void TokenPair_StoresAllProperties()
    {
        var accessToken = "access.jwt.token";
        var refreshToken = "refresh.jwt.token";
        var accessExpiresAt = DateTime.UtcNow.AddHours(3);
        var refreshExpiresAt = DateTime.UtcNow.AddHours(24);

        var tokenPair = new TokenPair(accessToken, refreshToken, accessExpiresAt, refreshExpiresAt);

        tokenPair.AccessToken.Should().Be(accessToken);
        tokenPair.RefreshToken.Should().Be(refreshToken);
        tokenPair.AccessTokenExpiresAt.Should().Be(accessExpiresAt);
        tokenPair.RefreshTokenExpiresAt.Should().Be(refreshExpiresAt);
    }

    [Fact]
    public void TokenPair_EqualityBasedOnValues()
    {
        var expiresAt = DateTime.UtcNow.AddHours(3);
        var refreshExpiresAt = DateTime.UtcNow.AddHours(24);

        var a = new TokenPair("tok", "ref", expiresAt, refreshExpiresAt);
        var b = new TokenPair("tok", "ref", expiresAt, refreshExpiresAt);

        a.Should().Be(b);
    }
}
