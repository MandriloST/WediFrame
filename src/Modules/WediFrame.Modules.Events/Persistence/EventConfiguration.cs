using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WediFrame.Modules.Events.Domain;

namespace WediFrame.Modules.Events.Persistence;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events", schema: "events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.GuestToken)
            .HasMaxLength(64) // Base64Url of 32 bytes = 43 chars; headroom
            .IsRequired();

        builder.Property(x => x.CoverPhotoKey)
            .HasMaxLength(512);

        // Guest page resolves the event by token — must be unique and fast.
        builder.HasIndex(x => x.GuestToken).IsUnique();

        // Host dashboard: "my events".
        builder.HasIndex(x => x.OwnerUserId);
    }
}
