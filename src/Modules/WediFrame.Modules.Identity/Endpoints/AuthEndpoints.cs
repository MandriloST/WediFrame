using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Identity.Contracts;
using WediFrame.Modules.Identity.Domain;
using WediFrame.Modules.Identity.Services;
using WediFrame.Shared.Audit;
using WediFrame.Shared.Email;
using WediFrame.Shared.Options;
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
        group.MapPost("/magic-link/request", MagicLinkRequestAsync).RequireRateLimiting(RateLimitPolicies.Auth);
        group.MapPost("/magic-link/consume", MagicLinkConsumeAsync).RequireRateLimiting(RateLimitPolicies.Auth);
        group.MapPost("/google", GoogleSignInAsync).RequireRateLimiting(RateLimitPolicies.Auth);
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

    // --- Magic link (passwordless) -----------------------------------------

    private static async Task<IResult> MagicLinkRequestAsync(
        MagicLinkRequest request,
        DbContext db,
        IEmailSender emailSender,
        IOptions<FrontendOptions> frontend,
        IOptions<MagicLinkOptions> magicOptions,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var options = magicOptions.Value;
        if (!options.Enabled)
        {
            return Results.NotFound();
        }

        var email = NormalizeEmail(request.Email);
        var language = NormalizeLanguage(request.Language);
        var now = timeProvider.GetUtcNow();

        // ALWAYS 200 with no detail past this point — never reveal whether the
        // email exists, is on cooldown, or whether a mail was actually sent.
        if (!IsValidEmail(email))
        {
            return Results.Ok();
        }

        // Per-email cooldown: a link was minted very recently → do nothing.
        var cooldownStart = now.AddSeconds(-options.PerEmailCooldownSeconds);
        var onCooldown = await db.Set<MagicLinkToken>()
            .AnyAsync(t => t.Email == email && t.CreatedAt > cooldownStart, ct);
        if (onCooldown)
        {
            return Results.Ok();
        }

        // Latest link wins: invalidate any still-valid outstanding links.
        var outstanding = await db.Set<MagicLinkToken>()
            .Where(t => t.Email == email && t.ConsumedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);
        foreach (var stale in outstanding)
        {
            stale.ConsumedAt = now;
        }

        var rawToken = NewUrlSafeToken();
        db.Set<MagicLinkToken>().Add(new MagicLinkToken
        {
            Id = Guid.NewGuid(),
            Email = email,
            TokenHash = HashToken(rawToken),
            Purpose = MagicLinkPurpose.Login,
            PreferredLanguage = language,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(options.TokenLifetimeMinutes),
        });
        await db.SaveChangesAsync(ct);

        // Frontend consume page (Step C): {AppBaseUrl}/auth/magic?token=...
        var appBase = frontend.Value.AppBaseUrl.TrimEnd('/');
        var link = $"{appBase}/auth/magic?token={rawToken}";
        var logger = loggerFactory.CreateLogger("Auth.MagicLink");

        // Dev convenience: the no-op email sender never logs the body, so surface
        // the link in the console when running in Development (dotnet run). Never
        // in Production — there the link only travels by email.
        if (IsDevelopmentEnvironment())
        {
            logger.LogInformation("DEV magic link for {Email}: {Link}", email, link);
        }

        try
        {
            var message = MagicLinkEmail.Build(language, email, link, options.TokenLifetimeMinutes);
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            // Don't leak failure to the caller; the token simply goes unused and expires.
            logger.LogError(ex, "Magic link email send failed.");
        }

        return Results.Ok();
    }

    private static async Task<IResult> MagicLinkConsumeAsync(
        MagicLinkConsumeRequest request,
        DbContext db,
        IOptions<MagicLinkOptions> magicOptions,
        ITokenIssuer tokenIssuer,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        if (!magicOptions.Value.Enabled)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return MagicLinkInvalid();
        }

        var hash = HashToken(request.Token);
        var now = timeProvider.GetUtcNow();

        var token = await db.Set<MagicLinkToken>().SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        // One error for unknown / expired / already-used — no oracle.
        if (token is null || !token.IsConsumable(now))
        {
            return MagicLinkInvalid();
        }

        token.ConsumedAt = now; // single use

        var user = await db.Set<User>().SingleOrDefaultAsync(u => u.Email == token.Email, ct);
        var registered = false;
        if (user is null)
        {
            // Registration via magic link: passwordless account, email proven.
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = token.Email,
                PasswordHash = null,
                EmailVerified = true,
                Role = UserRole.Host,
                PreferredLanguage = token.PreferredLanguage,
                CreatedAt = now,
            };
            db.Set<User>().Add(user);
            registered = true;
        }
        else if (!user.EmailVerified)
        {
            user.EmailVerified = true; // consuming the link proves ownership
        }

        var response = tokenIssuer.IssueSession(user, now);

        db.Set<AuditLogEntry>().Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            ActorUserId = user.Id,
            Action = registered ? "auth.registered_via_magic_link" : "auth.magic_link_consumed",
            EntityType = nameof(User),
            EntityId = user.Id.ToString(),
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Race (e.g. double-click consuming the same token): the account was
            // created concurrently under the unique email index. Re-resolve it and
            // issue a session in a clean unit of work.
            db.ChangeTracker.Clear();
            var existing = await db.Set<User>().SingleOrDefaultAsync(u => u.Email == token.Email, ct);
            if (existing is null)
            {
                return MagicLinkInvalid();
            }

            var retry = tokenIssuer.IssueSession(existing, now);
            await db.SaveChangesAsync(ct);
            return Results.Ok(retry);
        }

        return Results.Ok(response);
    }

    // --- Google Sign-In (approach B: verify ID token server-side) -----------

    private static async Task<IResult> GoogleSignInAsync(
        GoogleSignInRequest request,
        DbContext db,
        IOptions<GoogleAuthOptions> googleOptions,
        ITokenIssuer tokenIssuer,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var options = googleOptions.Value;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ClientId))
        {
            return Results.NotFound(); // feature off / not configured → frontend hides the button
        }

        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return GoogleInvalidToken();
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            // Verifies signature (Google JWKS), issuer, expiry, and audience == our ClientId.
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [options.ClientId] });
        }
        catch (InvalidJwtException)
        {
            return GoogleInvalidToken();
        }
        catch (Exception ex)
        {
            // Cert-fetch / network failure while verifying with Google → 401 (client retries).
            loggerFactory.CreateLogger("Auth.Google").LogError(ex, "Google token validation failed.");
            return GoogleInvalidToken();
        }

        // Only ever trust a Google-verified email — never link/create on an unverified one.
        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
        {
            return GoogleInvalidToken();
        }

        var email = NormalizeEmail(payload.Email);
        var subject = payload.Subject; // stable Google user id ("sub")
        var language = NormalizeLanguage(payload.Locale?.Split('-')[0]);
        var now = timeProvider.GetUtcNow();

        // 1) Match by Google subject first — survives an email change on Google's side.
        var user = await db.Set<User>().SingleOrDefaultAsync(u => u.GoogleSubjectId == subject, ct);
        string action;
        if (user is not null)
        {
            action = "auth.google_login";
        }
        else
        {
            // 2) Else match by the verified email and link Google to that account.
            user = await db.Set<User>().SingleOrDefaultAsync(u => u.Email == email, ct);
            if (user is not null)
            {
                if (string.IsNullOrEmpty(user.GoogleSubjectId))
                {
                    user.GoogleSubjectId = subject;
                    user.EmailVerified = true;
                    action = "auth.google_linked";
                }
                else
                {
                    // Email already linked to a different Google sub (anomalous). Email
                    // ownership is still proven, so log in without touching the link.
                    action = "auth.google_login";
                }
            }
            else
            {
                // 3) Brand-new account: passwordless, email verified by Google.
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    PasswordHash = null,
                    EmailVerified = true,
                    GoogleSubjectId = subject,
                    Role = UserRole.Host,
                    PreferredLanguage = language,
                    CreatedAt = now,
                };
                db.Set<User>().Add(user);
                action = "auth.registered_via_google";
            }
        }

        var response = tokenIssuer.IssueSession(user, now);

        db.Set<AuditLogEntry>().Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            ActorUserId = user.Id,
            Action = action,
            EntityType = nameof(User),
            EntityId = user.Id.ToString(),
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Race on first sign-in (unique Email / GoogleSubjectId). Re-resolve and issue.
            db.ChangeTracker.Clear();
            var existing = await db.Set<User>()
                .SingleOrDefaultAsync(u => u.GoogleSubjectId == subject || u.Email == email, ct);
            if (existing is null)
            {
                return GoogleInvalidToken();
            }

            var retry = tokenIssuer.IssueSession(existing, now);
            await db.SaveChangesAsync(ct);
            return Results.Ok(retry);
        }

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

    private static IResult MagicLinkInvalid()
        => Results.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "auth.magic_link_invalid");

    private static IResult GoogleInvalidToken()
        => Results.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "auth.google_invalid_token");

    /// <summary>32 random bytes, URL-safe base64 (no padding) — safe in a query string.</summary>
    private static string NewUrlSafeToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Base64 SHA-256 of the raw token — only this is stored.</summary>
    private static string HashToken(string rawToken)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static bool IsDevelopmentEnvironment()
        => string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development", StringComparison.OrdinalIgnoreCase);

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
