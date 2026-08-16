using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WediFrame.Modules.Media.Domain;

namespace WediFrame.Modules.Media.Persistence;

public sealed class MediaExportConfiguration : IEntityTypeConfiguration<MediaExport>
{
    public void Configure(EntityTypeBuilder<MediaExport> builder)
    {
        builder.ToTable("media_exports", schema: "media");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.ObjectKey).HasMaxLength(256);
        builder.Property(x => x.Error).HasMaxLength(256);

        // Worker poll: "next Pending (or stale Running), oldest first" per event.
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.EventId);
    }
}
