namespace WediFrame.Modules.Media.Contracts;

// Host-facing gallery management (M2). Unlike the guest gallery, the host sees
// HIDDEN items too (so they can be un-hidden) and gets per-item visibility +
// size, but never soft-deleted items. Auth is the host JWT + ownership check;
// error strings stay machine-readable codes mapped to i18n on the frontend.

/// <summary>
/// One page of the host gallery. Same offset pagination and deterministic order
/// as the guest gallery (CreatedAt desc, ObjectKey desc), but includes hidden
/// items and exposes each item's visibility so the host can toggle it.
/// </summary>
public sealed record HostGalleryResponse(
    List<HostGalleryItem> Items,
    int? NextOffset);

/// <summary>
/// A confirmed item for host management. <see cref="Url"/> is a presigned GET of
/// the original; <see cref="ThumbnailUrl"/> is set once the thumbnail worker runs
/// (null meanwhile — the grid falls back to the original, or a placeholder for
/// HEIC/HEIF). <see cref="Visibility"/> is "Visible" or "Hidden".
/// </summary>
public sealed record HostGalleryItem(
    Guid MediaId,
    string Type,
    string Url,
    string? ThumbnailUrl,
    string ContentType,
    string? GuestName,
    string Visibility,
    long SizeBytes,
    DateTimeOffset CreatedAt);

/// <summary>
/// Set an item's visibility. Value is the target state ("Visible" | "Hidden");
/// idempotent — setting the current state just returns the item unchanged.
/// </summary>
public sealed record UpdateMediaVisibilityRequest(string Visibility);

/// <summary>Echoes the item's visibility after a successful PATCH.</summary>
public sealed record HostMediaVisibilityResponse(Guid MediaId, string Visibility);
