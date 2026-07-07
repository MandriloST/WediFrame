namespace WediFrame.Modules.Events.Domain;

/// <summary>
/// A single event (wedding for now — event-type agnostic by design).
/// Guest access is granted solely by <see cref="GuestToken"/>; guests have no accounts.
/// Package linkage and derived dates (uploadEndsAt, expiresAt) arrive with Billing (M3).
/// </summary>
public sealed class Event
{
    public Guid Id { get; set; }

    /// <summary>
    /// Owning host's user id. Plain Guid on purpose — no cross-module FK/navigation,
    /// module boundaries stay clean (Identity owns users, Events only stores the reference).
    /// </summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Event type, e.g. "wedding". String keeps it open for future verticals.</summary>
    public string Type { get; set; } = EventTypes.Wedding;

    /// <summary>Display title, e.g. "Iva i Ivan, 23.6.2027.".</summary>
    public required string Title { get; set; }

    /// <summary>T0 — the date the host picked as start of the upload period.</summary>
    public DateOnly UploadStartDate { get; set; }

    /// <summary>R2 object key of the cover photo. Null until the host uploads one (M1, Media).</summary>
    public string? CoverPhotoKey { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Draft;

    /// <summary>
    /// Long, unguessable, URL-safe token. The ONLY key guests need — printed as QR.
    /// Rotatable by the host if the link leaks (M5).
    /// </summary>
    public required string GuestToken { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public enum EventStatus
{
    Draft = 0,
    Active = 1,
    UploadClosed = 2,
    Expired = 3,
    Deleted = 4,
}

public static class EventTypes
{
    public const string Wedding = "wedding";
}
