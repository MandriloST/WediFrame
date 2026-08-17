namespace WediFrame.Shared.Admin;

/// <summary>
/// Admin write-actions on an event that aren't destructive media moderation:
/// currently manual retention extension. Implemented by the Events module (which
/// owns the Event entity and its status rules), consumed by the Admin module
/// through this Shared contract — Admin references only Shared.
/// </summary>
public interface IAdminEventModeration
{
    /// <summary>
    /// Move an event's gallery expiry (<c>ExpiresAt</c>) to a later date. Only
    /// extension is allowed: the new date must be after both today and the current
    /// expiry. If the event was already Expired, it is revived to UploadClosed so
    /// the gallery becomes reachable again (uploads stay closed). Audited with the
    /// admin as actor. Draft/never-activated (no ExpiresAt) and Deleted events are
    /// rejected.
    /// </summary>
    Task<AdminRetentionResult> ExtendRetentionAsync(
        Guid eventId, DateOnly newExpiresAt, Guid adminUserId, CancellationToken ct);
}

/// <summary>Outcome of a retention-extension attempt.</summary>
public enum AdminRetentionOutcome
{
    Ok = 0,

    /// <summary>No such event.</summary>
    NotFound = 1,

    /// <summary>Event has no ExpiresAt yet (Draft / never activated) or is Deleted.</summary>
    NotActivated = 2,

    /// <summary>New date isn't strictly after both today and the current expiry.</summary>
    NotLater = 3,
}

/// <summary>Result of a retention-extension attempt; new state is set only on <see cref="AdminRetentionOutcome.Ok"/>.</summary>
public sealed record AdminRetentionResult(
    AdminRetentionOutcome Outcome,
    DateOnly? ExpiresAt = null,
    string? Status = null);
