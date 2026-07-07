using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Identity.Contracts;
using WediFrame.Modules.Identity.Domain;
using WediFrame.Modules.Identity.Services;

namespace WediFrame.Modules.Identity.Endpoints;

/// <summary>
/// Minimal host auth: register, login, refresh (rotating tokens), me.
/// Rate limiting on these routes arrives in M5; email verification is post-MVP.
/// </summary>
public static class AuthEndpoints
{
    private static readonly string[] SupportedLanguages = ["hr", "en"];

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapGet("/me", MeAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        DbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
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
            PasswordHash = "", // set right below (hasher needs the instance)
            Role = UserRole.Host,
            PreferredLanguage = NormalizeLanguage(request.Language),
            CreatedAt = now,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var (rawRefresh, refreshEntity) = tokenService.CreateRefreshToken(user.Id, now);
        db.Set<User>().Add(user);
        db.Set<RefreshToken>().Add(refreshEntity);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index race on Email — same outcome as the pre-check.
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, detail: "auth.email_taken");
        }

        return Results.Created("/api/v1/auth/me", BuildAuthResponse(user, tokenService, now, rawRefresh, refreshEntity));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        DbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Set<User>().SingleOrDefaultAsync(u => u.Email == email, ct);

        // Same error for unknown email and wrong password — no account enumeration.
        if (user is null)
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
        var (rawRefresh, refreshEntity) = tokenService.CreateRefreshToken(user.Id, now);
        db.Set<RefreshToken>().Add(refreshEntity);
        await db.SaveChangesAsync(ct);

        return Results.Ok(BuildAuthResponse(user, tokenService, now, rawRefresh, refreshEntity));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        DbContext db,
        ITokenService tokenService,
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
        var (rawRefresh, refreshEntity) = tokenService.CreateRefreshToken(stored.UserId, now);
        db.Set<RefreshToken>().Add(refreshEntity);
        await db.SaveChangesAsync(ct);

        return Results.Ok(BuildAuthResponse(stored.User, tokenService, now, rawRefresh, refreshEntity));
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

    private static AuthResponse BuildAuthResponse(
        User user, ITokenService tokenService, DateTimeOffset now, string rawRefresh, RefreshToken refreshEntity)
    {
        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(user, now);
        return new AuthResponse(accessToken, accessExpiresAt, rawRefresh, refreshEntity.ExpiresAt, ToUserResponse(user));
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
