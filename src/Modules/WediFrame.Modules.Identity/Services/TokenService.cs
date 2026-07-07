using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WediFrame.Modules.Identity.Domain;

namespace WediFrame.Modules.Identity.Services;

public interface ITokenService
{
    /// <summary>Signed JWT access token + its expiry.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, DateTimeOffset now);

    /// <summary>New opaque refresh token: raw value (for the client) + entity to persist.</summary>
    (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId, DateTimeOffset now);

    /// <summary>Hash used to look up a stored refresh token.</summary>
    string HashRefreshToken(string rawToken);
}

public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private static readonly JsonWebTokenHandler Handler = new();

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, DateTimeOffset now)
    {
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", user.Role.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return (Handler.CreateToken(descriptor), expiresAt);
    }

    public (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId, DateTimeOffset now)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashRefreshToken(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays),
        };

        return (rawToken, entity);
    }

    public string HashRefreshToken(string rawToken)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
