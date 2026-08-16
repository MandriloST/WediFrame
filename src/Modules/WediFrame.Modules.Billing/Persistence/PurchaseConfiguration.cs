using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WediFrame.Modules.Billing.Domain;

namespace WediFrame.Modules.Billing.Persistence;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases", schema: "billing");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.PaymentProvider).HasMaxLength(40);
        builder.Property(x => x.PaymentReference).HasMaxLength(200);

        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.CompanyOib).HasMaxLength(20);
        builder.Property(x => x.CompanyAddress).HasMaxLength(300);

        builder.Property(x => x.FiscalProvider).HasMaxLength(40);
        builder.Property(x => x.FiscalInvoiceNumber).HasMaxLength(60);
        builder.Property(x => x.FiscalJir).HasMaxLength(60);
        builder.Property(x => x.FiscalZki).HasMaxLength(60);
        builder.Property(x => x.FiscalStatus).HasMaxLength(20);

        // Store enum as string — readable in the DB, stable across reordering.
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // "Purchases for this event" + reconciling a gateway callback by reference.
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => x.PaymentReference);
    }
}
