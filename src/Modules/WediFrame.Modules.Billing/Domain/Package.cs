namespace WediFrame.Modules.Billing.Domain;

/// <summary>
/// A purchasable package: price + all per-event limits, stored as DATA (never
/// hardcoded in enforcement code — see PROJECT.md §3). The 5 official packages
/// are seeded via EF <c>HasData</c> in <see cref="Persistence.PackageConfiguration"/>.
/// Event linkage (Purchase) and limit enforcement arrive later in M3.
///
/// Byte limits are binary (1024-based, GiB/MiB) to match the frontend caps
/// (guestApi PHOTO_MAX_BYTES / VIDEO_MAX_BYTES). Months are stored as days
/// (30/60/120) — a conscious MVP simplification, revisitable if calendar-month
/// precision is needed.
/// </summary>
public sealed class Package
{
    public Guid Id { get; set; }

    /// <summary>Stable machine key (e.g. "essential"). The pricing page localizes
    /// display names by this slug; <see cref="Name"/> is only a canonical reference.</summary>
    public required string Slug { get; set; }

    /// <summary>Canonical display name for admin/reference (e.g. "Essential").</summary>
    public required string Name { get; set; }

    /// <summary>Price in minor units (cents). 0 = Free/Trial.</summary>
    public int PriceCents { get; set; }

    /// <summary>ISO 4217 currency code. EUR for the HR market; multi-currency ready.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Max number of photos per event (total).</summary>
    public int MaxPhotoCount { get; set; }

    /// <summary>Max total bytes of video per event.</summary>
    public long MaxVideoTotalBytes { get; set; }

    /// <summary>Max total upload bytes per event (photos + video).</summary>
    public long MaxTotalBytes { get; set; }

    /// <summary>Max bytes for a single file (2 GB — the largest allowed video).</summary>
    public long MaxFileBytes { get; set; }

    /// <summary>Upload window length from T0, in days.</summary>
    public int UploadPeriodDays { get; set; }

    /// <summary>Storage/retention length from T0, in days.</summary>
    public int RetentionDays { get; set; }

    /// <summary>Active packages are offered; archived ones stay for old purchases only.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display order on the pricing page (ascending).</summary>
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
