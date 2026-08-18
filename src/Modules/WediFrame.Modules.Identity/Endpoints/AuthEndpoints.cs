using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Identity.Contracts;
using WediFrame.Modules.Identity.Domain;
using WediFrame.Modules.Identity.Services;
using WediFrame.Shared.RateLimiting;

namespace WediFrame.Modules.Identity.Endpoints;

/// <summary>
/// Minimal host auth: register, login, refresh (rotating tokens), me.
/// Session issuance is centralized in <see cref="ITokenIssuer"/> so Google and
/// magic-link entry points (arriving in later steps) share one code path.
/// </summary>
public static class AuthEndpoints
{
    private static readonly string[] SupportedLanguages = ["hr", "en"];

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync).RequireRateLimiting(RateLimitPolicies.Auth);
        group.MapPost("/login", LoginAsync).RequireRateLimiting(RateLimitPolicies.Auth);
        group.MapPost("/refresh", RefreshAsync).RequireRateLimiting(RateLimitPolicies.Auth);
        group.MapGet("/me", MeAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        DbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenIssuer tokenIssuer,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var errors = new Dictionary<string, string[]>();

        if (!IsValidEmail(email))
        {
            errors["email"] = ["auth.email_invalid"];
        }

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8 || request.Password.Length > 128)
        {
            errors["password"] = ["auth.password_length"]; // 8–128 characters
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (await db.Set<User>().AnyAsync(u => u.Email == email, ct))
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, detail: "auth.email_taken");
        }

        var now = timeProvider.GetUtcNow();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = null, // set right below (hasher needs the instance)
            EmailVerified = false, // no email-verification flow for password signup (MVP)
            Role = UserRole.Host,
            PreferredLanguage = NormalizeLanguage(request.Language),
            CreatedAt = now,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Set<User>().Add(user);
        var response = tokenIssuer.IssueSession(user, now);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index race on Email — same outcome as the pre-check.
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, detail: "auth.email_taken");
        }

        return Results.Created("/api/v1/auth/me", response);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        DbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenIssuer tokenIssuer,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Set<User>().SingleOrDefaultAsync(u => u.Email == email, ct);

        // Same error for unknown email, passwordless account, and wrong password —
        // no account enumeration, and no hint that an account is Google/magic-link only.
        if (user is null || user.PasswordHash is null)
        {
            return InvalidCredentials();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password ?? "");
        if (verification == PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
        }

        var now = timeProvider.GetUtcNow();
        var response = tokenIssuer.IssueSession(user, now);
        await db.SaveChangesAsync(ct);

        return Results.Ok(response);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        DbContext db,
        ITokenService tokenService,
        ITokenIssuer tokenIssuer,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return InvalidCredentials();
        }

        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var stored = await db.Set<RefreshToken>()
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        var now = timeProvider.GetUtcNow();
        if (stored is null || stored.User is null || !stored.IsActive(now))
        {
            return InvalidCredentials();
        }

        // Rotation: revoke the used token, issue a fresh pair.
        stored.RevokedAt = now;
        var response = tokenIssuer.IssueSession(stored.User, now);
        await db.SaveChangesAsync(ct);

        return Results.Ok(response);
    }

    private static async Task<IResult> MeAsync(ClaimsPrincipal principal, DbContext db, CancellationToken ct)
    {
        var sub = principal.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Set<User>().SingleOrDefaultAsync(u => u.Id == userId, ct);
        return user is null
            ? Results.Unauthorized()
            : Results.Ok(ToUserResponse(user));
    }

    private static UserResponse ToUserResponse(User user)
        => new(user.Id, user.Email, user.Role.ToString(), user.PreferredLanguage);

    private static IResult InvalidCredentials()
        => Results.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "auth.invalid_credentials");

    private static string NormalizeEmail(string? email)
        => (email ?? "").Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
        => email.Length is >= 5 and <= 320
           && email.IndexOf('@') is var at
           && at > 0
           && at < email.Length - 3
           && email.LastIndexOf('.') > at;

    private static string NormalizeLanguage(string? language)
    {
        var normalized = (language ?? "hr").Trim().ToLowerInvariant();
        return SupportedLanguages.Contains(normalized) ? normalized : "hr";
    }
}
