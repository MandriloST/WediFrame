namespace WediFrame.Shared.Audit;

/// <summary>
/// Cross-cutting audit trail: deletions, admin actions, settings changes.
/// Lives in the "shared" schema. This is intentionally the only entity in the
/// initial migration — feature entities arrive with their modules (M1+).
/// </summary>
public sealed class AuditLogEntry
{
    public Guid Id { get; set; }

    /// <summary>When the action occurred (UTC).</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>User id of the actor, or null for system/background jobs.</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>Machine-readable action name, e.g. "event.deleted", "media.hidden".</summary>
    public required string Action { get; set; }

    /// <summary>Type of the affected entity, e.g. "Event", "MediaItem".</summary>
    public string? EntityType { get; set; }

    /// <summary>Id of the affected entity (string to stay type-agnostic).</summary>
    public string? EntityId { get; set; }

    /// <summary>Optional JSON payload with extra context (stored as jsonb).</summary>
    public string? Details { get; set; }
}
