using Microsoft.EntityFrameworkCore;
using WediFrame.Infrastructure.Persistence;
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

app.UseExceptionHandler();
app.UseStatusCodePages();

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

app.Run();
