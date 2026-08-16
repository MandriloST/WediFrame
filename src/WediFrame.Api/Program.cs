using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WediFrame.Api.RateLimiting;
using WediFrame.Infrastructure.Persistence;
using WediFrame.Infrastructure.Storage;
using WediFrame.Infrastructure.Imaging;
using WediFrame.Infrastructure.Email;
using WediFrame.Modules.Admin;
using WediFrame.Modules.Billing;
using WediFrame.Modules.Events;
using WediFrame.Modules.Identity;
using WediFrame.Modules.Media;
using WediFrame.Modules.Partners;
using WediFrame.Modules.Retention;
using WediFrame.Shared.Modules;

var builder = WebApplication.CreateBuilder(args);

// --- Persistence -------------------------------------------------------------
// Connection string: appsettings / user-secrets locally, env var on Railway
// (ConnectionStrings__Database).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Modules depend on the base DbContext (they never reference Infrastructure).
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.AddSingleton(TimeProvider.System);

// Cloudflare R2 (S3-compatible). Lazy: API boots without R2 config,
// the first storage call throws a clear error instead.
builder.Services.AddR2Storage(builder.Configuration);

/// Thumbnail generation (libvips via NetVips). A technology concern like R2;
// the Media module's background worker depends only on IThumbnailGenerator.
builder.Services.AddImaging();

// Transactional email (SMTP). No-op logging sender until the "Email" section is
// configured, so the API boots and nothing is sent by accident. Consumed by the
// retention reminder (Retention worker) via IEmailSender.
builder.Services.AddEmail(builder.Configuration);

// --- AuthN/AuthZ ---------------------------------------------------------------
// JWT bearer for host/admin endpoints. Guests are authorized by event token
// (Events module, M1) and never touch this scheme.
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep "sub", "email", "role" as-is
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"] ?? "")),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// CORS: the Next.js frontend calls the API from the browser (guest page, dashboard).
// Origins come from Frontend:AllowedOrigins (Frontend__AllowedOrigins__0 on Railway).
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Behind Railway's proxy the client IP arrives in X-Forwarded-For. Map it onto
// RemoteIpAddress so rate limiting keys per real device, not the proxy. The
// proxy IP is dynamic, so we trust the forwarded header (used only for throttle
// keying + logging, never for auth decisions).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// Per-IP rate limiting on public surfaces (auth brute-force, guest, upload).
// Policies opt in per endpoint via .RequireRateLimiting(RateLimitPolicies.*).
builder.Services.AddWediFrameRateLimiting(builder.Configuration);

// --- Modules (explicit list — order matters only for readability) ------------
IModule[] modules =
[
    new IdentityModule(),
    new EventsModule(),
    new MediaModule(),
    new BillingModule(),
    new PartnersModule(),
    new RetentionModule(),
    new AdminModule(),
];

foreach (var module in modules)
{
    module.RegisterServices(builder.Services, builder.Configuration);
}

// --- API plumbing -------------------------------------------------------------
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Must run before anything that reads the client IP (rate limiting) so
// X-Forwarded-For is applied to RemoteIpAddress.
app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
}

// Liveness probe for Railway/uptime checks. DB readiness check arrives in M1.
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "wediframe-api",
    modules = modules.Select(m => m.Name),
}));

// All feature endpoints live under /api/v1 and are mapped by their module.
var apiV1 = app.MapGroup("/api/v1");
foreach (var module in modules)
{
    module.MapEndpoints(apiV1);
}

await app.RunAsync();
