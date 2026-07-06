using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WediFrame.Shared.Audit;

namespace WediFrame.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log", schema: "shared");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EntityType).HasMaxLength(64);
        builder.Property(x => x.EntityId).HasMaxLength(64);

        builder.Property(x => x.Details).HasColumnType("jsonb");

        // Typical queries: "what happened on entity X" and "recent activity".
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.OccurredAt);
    }
}
