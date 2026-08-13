using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WediFrame.Modules.Billing.Domain;

namespace WediFrame.Modules.Billing.Persistence;

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    // Binary units (match the frontend caps and file sizes on disk).
    private const long Mb = 1024L * 1024;
    private const long Gb = 1024L * 1024 * 1024;

    // Per single video file, all packages (PROJECT.md §3, confirmed 2026-07-06).
    private const long MaxVideoFileBytes = 2 * Gb;

    // Fixed timestamp so the HasData seed is deterministic across migrations.
    private static readonly DateTimeOffset SeededAt = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("packages", schema: "billing");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();

        // Lookups happen by the stable slug; must be unique.
        builder.HasIndex(x => x.Slug).IsUnique();

        // The 5 official packages (PROJECT.md §3). Limits live as DATA here,
        // never hardcoded in enforcement code. Months are stored as days.
        builder.HasData(
            new Package
            {
                Id = new Guid("11111111-1111-1111-1111-000000000001"),
                Slug = "free",
                Name = "Free",
                PriceCents = 0,
                Currency = "EUR",
                MaxPhotoCount = 50,
                MaxVideoTotalBytes = 50 * Mb,
                MaxTotalBytes = 250 * Mb,
                MaxFileBytes = MaxVideoFileBytes,
                UploadPeriodDays = 2,
                RetentionDays = 7,
                IsActive = true,
                SortOrder = 0,
                CreatedAt = SeededAt,
            },
            new Package
            {
                Id = new Guid("11111111-1111-1111-1111-000000000002"),
                Slug = "essential",
                Name = "Essential",
                PriceCents = 2500,
                Currency = "EUR",
                MaxPhotoCount = 500,
                MaxVideoTotalBytes = 5 * Gb,
                MaxTotalBytes = 10 * Gb,
                MaxFileBytes = MaxVideoFileBytes,
                UploadPeriodDays = 30,
                RetentionDays = 90,
                IsActive = true,
                SortOrder = 1,
                CreatedAt = SeededAt,
            },
            new Package
            {
                Id = new Guid("11111111-1111-1111-1111-000000000003"),
                Slug = "classic",
                Name = "Classic",
                PriceCents = 4000,
                Currency = "EUR",
                MaxPhotoCount = 1500,
                MaxVideoTotalBytes = 15 * Gb,
                MaxTotalBytes = 20 * Gb,
                MaxFileBytes = MaxVideoFileBytes,
                UploadPeriodDays = 60,
                RetentionDays = 365,
                IsActive = true,
                SortOrder = 2,
                CreatedAt = SeededAt,
            },
            new Package
            {
                Id = new Guid("11111111-1111-1111-1111-000000000004"),
                Slug = "premium",
                Name = "Premium",
                PriceCents = 8000,
                Currency = "EUR",
                MaxPhotoCount = 5000,
                MaxVideoTotalBytes = 40 * Gb,
                MaxTotalBytes = 50 * Gb,
                MaxFileBytes = MaxVideoFileBytes,
                UploadPeriodDays = 120,
                RetentionDays = 365,
                IsActive = true,
                SortOrder = 3,
                CreatedAt = SeededAt,
            },
            new Package
            {
                Id = new Guid("11111111-1111-1111-1111-000000000005"),
                Slug = "brzi-i-zestoki",
                Name = "Brzi i žestoki",
                PriceCents = 5000,
                Currency = "EUR",
                MaxPhotoCount = 5000,
                MaxVideoTotalBytes = 40 * Gb,
                MaxTotalBytes = 50 * Gb,
                MaxFileBytes = MaxVideoFileBytes,
                UploadPeriodDays = 14,
                RetentionDays = 60,
                IsActive = true,
                SortOrder = 4,
                CreatedAt = SeededAt,
            });
    }
}
