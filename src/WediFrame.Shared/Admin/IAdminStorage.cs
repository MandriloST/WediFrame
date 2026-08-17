namespace WediFrame.Shared.Admin;

/// <summary>
/// Read-only storage aggregates over all media, for the admin overview / storage
/// report. Implemented by the Media module (owns MediaItem), consumed by the Admin
/// module through this Shared contract. Counts only Confirmed, non-deleted items —
/// the same rule galleries and quotas use — so numbers match what's actually stored.
/// </summary>
public interface IAdminStorage
{
    /// <summary>System-wide totals (bytes + item counts).</summary>
    Task<AdminStorageTotals> GetTotalsAsync(CancellationToken ct);

    /// <summary>Heaviest events by stored bytes, descending, capped at <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<AdminEventStorage>> TopEventsByStorageAsync(int limit, CancellationToken ct);
}

/// <summary>System-wide storage totals.</summary>
public sealed record AdminStorageTotals(
    long TotalBytes,
    int ItemCount,
    int PhotoCount,
    int VideoCount);

/// <summary>Stored bytes + item count for a single event.</summary>
public sealed record AdminEventStorage(Guid EventId, long Bytes, int ItemCount);
