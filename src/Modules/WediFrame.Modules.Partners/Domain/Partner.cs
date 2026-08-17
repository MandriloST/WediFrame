namespace WediFrame.Modules.Partners.Domain;

/// <summary>
/// A referral partner who hands bonus codes to couples (photographers, venues,
/// planners…). Commission is tracked/paid manually in the MVP (PROJECT.md), so no
/// payout fields live here yet — only identity + contact for the per-partner report.
/// </summary>
public sealed class Partner
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public PartnerType Type { get; set; } = PartnerType.Photographer;

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    /// <summary>Free-form internal note (deal terms, etc.). Not shown to couples.</summary>
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Partner category (ARCHITECTURE.md §Domain). "Other" keeps it open-ended.</summary>
public enum PartnerType
{
    Photographer = 0,
    Videographer = 1,
    Venue = 2,
    Planner = 3,
    Organizer = 4,
    Other = 5,
}
