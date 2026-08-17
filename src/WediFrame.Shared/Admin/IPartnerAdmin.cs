namespace WediFrame.Shared.Admin;

/// <summary>
/// Admin management of partners and their bonus codes, plus the per-partner report.
/// Implemented by the Partners module (owns the entities), consumed by the Admin
/// module through this Shared contract — Admin references only Shared.
///
/// Redemption counts are populated once checkout attribution lands (P2); until then
/// the report shows the codes with zero redemptions.
/// </summary>
public interface IPartnerAdmin
{
    Task<PartnerPage> ListPartnersAsync(string? search, int page, int pageSize, CancellationToken ct);

    Task<PartnerRecord> CreatePartnerAsync(PartnerInput input, CancellationToken ct);

    Task<PartnerDetail?> GetPartnerAsync(Guid partnerId, CancellationToken ct);

    Task<CreateCodeResult> CreateCodeAsync(Guid partnerId, BonusCodeInput input, CancellationToken ct);

    /// <summary>Enable/disable a code. Null if the code (under that partner) doesn't exist.</summary>
    Task<BonusCodeRecord?> SetCodeActiveAsync(Guid partnerId, Guid codeId, bool active, CancellationToken ct);

    /// <summary>Per-partner redemption report. Null if the partner doesn't exist.</summary>
    Task<PartnerReport?> GetReportAsync(Guid partnerId, CancellationToken ct);
}

// ── Inputs ──────────────────────────────────────────────────────────────────

public sealed record PartnerInput(
    string Name, string Type, string? ContactEmail, string? ContactPhone, string? Notes);

public sealed record BonusCodeInput(
    string Code, string DiscountType, int DiscountValue, int? MaxRedemptions, DateOnly? ExpiresAt);

// ── Records ─────────────────────────────────────────────────────────────────

public sealed record PartnerRecord(
    Guid Id, string Name, string Type, string? ContactEmail, string? ContactPhone,
    int CodeCount, DateTimeOffset CreatedAt);

public sealed record PartnerPage(IReadOnlyList<PartnerRecord> Items, int Total);

public sealed record BonusCodeRecord(
    Guid Id, string Code, string DiscountType, int DiscountValue,
    int? MaxRedemptions, DateOnly? ExpiresAt, int RedemptionCount, bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record PartnerDetail(
    Guid Id, string Name, string Type, string? ContactEmail, string? ContactPhone, string? Notes,
    DateTimeOffset CreatedAt, IReadOnlyList<BonusCodeRecord> Codes);

/// <summary>Aggregated redemption view for one partner.</summary>
public sealed record PartnerReport(
    Guid PartnerId, string Name, int CodeCount, int TotalRedemptions,
    IReadOnlyList<BonusCodeRecord> Codes);

/// <summary>Outcome of creating a code (uniqueness + validation).</summary>
public enum CreateCodeOutcome { Ok = 0, PartnerNotFound = 1, DuplicateCode = 2, Invalid = 3 }

public sealed record CreateCodeResult(CreateCodeOutcome Outcome, BonusCodeRecord? Code = null);
