namespace WediFrame.Shared.Media;

/// <summary>
/// Cross-module port for PHYSICALLY erasing all of an event's media — R2 objects
/// (originals, thumbnails, in-flight multiparts, export ZIPs) AND the database
/// rows. The implementation lives in the Media module (it owns those entities);
/// this contract lives in Shared so callers depend on the abstraction, not the
/// module — exactly like <see cref="Storage.IObjectStorage"/>.
///
/// Two callers, no dependency cycle:
///   • Retention worker — after the grace period (M4, Phase 2).
///   • Events host endpoint — DELETE /events/{id}, right to erasure (Phase 3).
/// Both would otherwise need a compile-time reference to Media (Media already
/// references Events), which the shared port avoids.
///
/// Idempotent: an R2 delete is a no-op on a missing key and the row delete is a
/// filtered bulk delete, so re-running after a partial failure is safe. The
/// caller flips the event to Deleted only once purge has fully succeeded.
/// </summary>
public interface IEventMediaPurge
{
    Task<EventMediaPurgeResult> PurgeAsync(Guid eventId, CancellationToken ct = default);
}

/// <summary>Counts from one purge, for logging and the audit trail.</summary>
public sealed record EventMediaPurgeResult(int MediaDeleted, int ExportsDeleted, int ObjectsDeleted)
{
    public static readonly EventMediaPurgeResult Empty = new(0, 0, 0);
}
