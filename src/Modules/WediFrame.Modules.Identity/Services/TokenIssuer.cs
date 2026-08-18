using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Identity.Contracts;
using WediFrame.Modules.Identity.Domain;

namespace WediFrame.Modules.Identity.Services;

/// <summary>
/// Single seam for minting a WediFrame session (access JWT + rotating refresh
/// token) and shaping the <see cref="AuthResponse"/>. Every entry point that
/// logs a user in — password login, refresh, Google, magic link — goes through
/// here so there is exactly one code path for session creation.
/// </summary>
public interface ITokenIssuer
{
    /// <summary>
    /// Creates and tracks a new refresh token for <paramref name="user"/> and
    /// returns the full auth response. Does NOT call SaveChanges — the caller
    /// owns the transaction (e.g. register persists the new User in the same
    /// commit; refresh revokes the old token first).
    /// </summary>
    AuthResponse IssueSession(User user, DateTimeOffset now);
}

public sealed class TokenIssuer(DbContext db, ITokenService tokenService) : ITokenIssuer
{
    public AuthResponse IssueSession(User user, DateTimeOffset now)
    {
        var (rawRefresh, refreshEntity) = tokenService.CreateRefreshToken(user.Id, now);
        db.Set<RefreshToken>().Add(refreshEntity);

        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(user, now);

        return new AuthResponse(
            accessToken,
            accessExpiresAt,
            rawRefresh,
            refreshEntity.ExpiresAt,
            new UserResponse(user.Id, user.Email, user.Role.ToString(), user.PreferredLanguage));
    }
}
