using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WediFrame.Modules.Partners.Domain;

namespace WediFrame.Modules.Partners.Persistence;

public sealed class BonusCodeConfiguration : IEntityTypeConfiguration<BonusCode>
{
    public void Configure(EntityTypeBuilder<BonusCode> b)
    {
        b.ToTable("bonus_code", "partners");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).IsRequired().HasMaxLength(64);
        b.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(32);

        // Unique code across the system (checkout looks it up by code, P2).
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.PartnerId);
    }
}
