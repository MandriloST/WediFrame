using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WediFrame.Shared.Admin;

namespace WediFrame.Modules.Admin.Services;

/// <summary>
/// On startup, promotes the configured <c>Admin:BootstrapEmails</c> to the Admin role
/// via the Identity module's <see cref="IAdminIdentity"/> contract. Idempotent and safe
/// to run on every boot; promotes only EXISTING users (register first, add the email to
/// config, restart). Best-effort — a failure here never blocks API startup.
/// </summary>
public sealed class AdminBootstrapService(
    IServiceScopeFactory scopeFactory,
    IOptions<AdminOptions> options,
    ILogger<AdminBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var emails = options.Value.BootstrapEmails ?? [];
        if (emails.Length == 0)
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var identity = scope.ServiceProvider.GetRequiredService<IAdminIdentity>();
            var promoted = await identity.PromoteEmailsToAdminAsync(emails, cancellationToken);
            if (promoted > 0)
            {
                logger.LogInformation("Admin bootstrap: promoted {Count} user(s) to Admin.", promoted);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Admin bootstrap failed (continuing startup).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
