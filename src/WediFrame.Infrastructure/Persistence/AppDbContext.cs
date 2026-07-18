using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events;
using WediFrame.Modules.Identity;
using WediFrame.Modules.Media;
using WediFrame.Shared.Audit;

namespace WediFrame.Infrastructure.Persistence;

/// <summary>
/// Single DbContext for the modular monolith (one migration history, one deploy unit).
/// Module boundaries are kept via PostgreSQL schemas: each module's entities live in
/// its own schema (identity, events, media, billing, partners, shared...).
/// Modules access data through the base <see cref="DbContext"/> registered in DI
/// (see Program.cs) — they never reference this assembly.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Shared/infrastructure entities.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Module entities — one line per module as they gain entities (M1+).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityModule).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsModule).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaModule).Assembly);
    }
}
