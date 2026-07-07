using System.Security.Claims;

namespace WediFrame.Shared.Auth;

/// <summary>
/// Reads the authenticated host's user id from the JWT "sub" claim.
/// (MapInboundClaims is off in the API host, so "sub" survives as-is.)
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue("sub"), out var id) ? id : null;
}
