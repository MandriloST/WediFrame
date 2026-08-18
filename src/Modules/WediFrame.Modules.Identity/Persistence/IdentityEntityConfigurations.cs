using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WediFrame.Modules.Identity.Domain;

namespace WediFrame.Modules.Identity.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", schema: "identity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(320) // RFC upper bound; normalized lowercase in app code
            .IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();

        // Nullable: Google-only / magic-link-only accounts have no password.
        builder.Property(x => x.PasswordHash)
            .HasMaxLength(512);

        builder.Property(x => x.EmailVerified)
            .HasDefaultValue(false);

        builder.Property(x => x.GoogleSubjectId)
            .HasMaxLength(255);

        // Unique when set. On PostgreSQL NULLs are distinct, so many
        // password/magic-link accounts (GoogleSubjectId == null) coexist freely.
        builder.HasIndex(x => x.GoogleSubjectId)
            .IsUnique();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(x => x.PreferredLanguage)
            .HasMaxLength(8);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", schema: "identity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .HasMaxLength(64) // base64 SHA-256 = 44 chars; headroom
            .IsRequired();

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
