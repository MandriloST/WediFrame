namespace WediFrame.Shared.Admin;

/// <summary>
/// Admin moderation over ANY event's media (no ownership check — admin is trusted).
/// Implemented by the Media module, consumed by the Admin module through this Shared
/// contract. Actions are audited with the admin as actor and distinct action codes
/// (media.*_by_admin) so the trail separates admin moderation from host curation.
/// Delete is a SOFT delete (same as host): retention physically removes it later.
/// </summary>
public interface IAdminMedia
{
    /// <summary>Confirmed, non-deleted items (incl. hidden), newest first, offset-paged.</summary>
    Task<AdminMediaPage> ListAsync(Guid eventId, int offset, int limit, CancellationToken ct);

    /// <summary>Hide/unhide one item. Null if the item doesn't exist (or is deleted). Idempotent.</summary>
    Task<AdminMediaVisibilityResult?> SetVisibilityAsync(
        Guid eventId, Guid mediaId, bool hidden, Guid adminUserId, CancellationToken ct);

    /// <summary>Soft-delete one item. False if it doesn't exist. Idempotent.</summary>
    Task<bool> SoftDeleteAsync(Guid eventId, Guid mediaId, Guid adminUserId, CancellationToken ct);
}

/// <summary>One media item for the admin gallery (display + visibility).</summary>
public sealed record AdminMediaItem(
    Guid MediaId,
    string Type,
    string Url,
    string? ThumbnailUrl,
    string ContentType,
    string? GuestName,
    string Visibility,
    long SizeBytes,
    DateTimeOffset CreatedAt);

/// <summary>A page of media plus the next offset (null when exhausted).</summary>
public sealed record AdminMediaPage(IReadOnlyList<AdminMediaItem> Items, int? NextOffset);

/// <summary>Result of a visibility toggle.</summary>
public sealed record AdminMediaVisibilityResult(Guid MediaId, string Visibility);
