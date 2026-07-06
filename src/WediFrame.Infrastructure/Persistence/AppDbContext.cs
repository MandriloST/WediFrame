using Microsoft.EntityFrameworkCore;
using WediFrame.Shared.Audit;

namespace WediFrame.Infrastructure.Persistence;

/// <summary>
/// Single DbContext for the modular monolith (one migration history, one deploy unit).
/// Module boundaries are kept via PostgreSQL schemas: each module's entities live in
/// its own schema (identity, events, media, billing, partners, shared...).
/// When modules gain entities (M1+), add their configuration assemblies below.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurations from this assembly (shared/infrastructure entities).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // M1+: apply module configuration assemblies here, e.g.
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsModule).Assembly);
    }
}
