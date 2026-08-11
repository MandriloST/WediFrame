using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WediFrame.Modules.Media.Domain;

namespace WediFrame.Modules.Media.Persistence;

public sealed class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.ToTable("media_items", schema: "media");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.ObjectKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.FileName).HasMaxLength(PhotoRules.MaxFileNameLength);
        builder.Property(x => x.GuestName).HasMaxLength(PhotoRules.MaxGuestNameLength);

        builder.Property(x => x.UploadStatus)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.Visibility)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.ThumbnailKey).HasMaxLength(256);

        // Stored as text; default backfills existing rows to Pending on migration.
        builder.Property(x => x.ThumbnailStatus)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(MediaThumbnailStatus.Pending);

        // Gallery queries: newest first within an event, filtered by status/visibility.
        builder.HasIndex(x => new { x.EventId, x.CreatedAt });
        builder.HasIndex(x => new { x.EventId, x.UploadStatus });

        // Thumbnail worker poll: photos, confirmed, still pending.
        builder.HasIndex(x => new { x.Type, x.UploadStatus, x.ThumbnailStatus });

        // One object key = one item; also makes confirm/cleanup lookups cheap.
        builder.HasIndex(x => x.ObjectKey).IsUnique();
    }
}
