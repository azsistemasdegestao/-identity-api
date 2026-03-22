using IdentityApi.Domain.Entities;
using IdentityApi.Domain.ValueObjects;

namespace IdentityApi.Domain.Services;

public interface ITokenService
{
    TokenPair GenerateTokenPair(User user);
    Guid? ValidateRefreshToken(string refreshToken);
}
