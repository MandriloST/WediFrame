using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Billing.Domain;

namespace WediFrame.Modules.Billing.Services;

/// <summary>
/// The Billing module's public contract for reading packages. Other modules
/// (Events now) consume THIS instead of touching the Package entity directly —
/// keeps module boundaries explicit (mirrors Events' IGuestEventAccess).
/// Registered by <see cref="BillingModule"/>.
/// </summary>
public interface IPackageCatalog
{
    /// <summary>Resolve an active package by its stable slug, or null if unknown/inactive.</summary>
    Task<PackageInfo?> GetBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Resolve any package by id (active or archived — old events keep their terms).</summary>
    Task<PackageInfo?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Read-only slice of a package that other modules need.</summary>
public sealed record PackageInfo(
    Guid Id,
    string Slug,
    string Name,
    int PriceCents,
    string Currency,
    int MaxPhotoCount,
    long MaxVideoTotalBytes,
    long MaxTotalBytes,
    long MaxFileBytes,
    int UploadPeriodDays,
    int RetentionDays)
{
    /// <summary>Free/Trial = no charge → activates without payment.</summary>
    public bool IsFree => PriceCents == 0;
}

public sealed class PackageCatalog(DbContext db) : IPackageCatalog
{
    public Task<PackageInfo?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var normalized = (slug ?? "").Trim().ToLowerInvariant();
        return db.Set<Package>()
            .Where(p => p.Slug == normalized && p.IsActive)
            .Select(Projection)
            .SingleOrDefaultAsync(ct);
    }

    public Task<PackageInfo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Set<Package>()
            .Where(p => p.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(ct);

    private static readonly System.Linq.Expressions.Expression<Func<Package, PackageInfo>> Projection =
        p => new PackageInfo(
            p.Id, p.Slug, p.Name, p.PriceCents, p.Currency,
            p.MaxPhotoCount, p.MaxVideoTotalBytes, p.MaxTotalBytes, p.MaxFileBytes,
            p.UploadPeriodDays, p.RetentionDays);
}
