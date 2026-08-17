namespace WediFrame.Shared.Admin;

/// <summary>
/// Read-only admin view over ALL events (any owner, any status — including Deleted,
/// for the audit trail). Implemented by the Events module, consumed by the Admin
/// module through this Shared contract (same pattern as IAdminUserDirectory).
/// Owner email is NOT resolved here — Events doesn't reach Identity; the Admin
/// endpoint resolves emails via IUserDirectory.
/// </summary>
public interface IAdminEventDirectory
{
    /// <summary>Paged, filtered list of events, newest first.</summary>
    Task<AdminEventPage> ListAsync(AdminEventQuery query, CancellationToken ct);

    /// <summary>Single event with a bit more detail (guest token/URL), or null.</summary>
    Task<AdminEventDetail?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Count of events grouped by status name (Draft/Active/…), for the overview.</summary>
    Task<IReadOnlyDictionary<string, int>> GetStatusCountsAsync(CancellationToken ct);

    /// <summary>Resolve titles for a set of event ids (storage report enrichment).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct);
}

/// <summary>Filter/paging input.</summary>
/// <param name="Search">Case-insensitive title substring, or null.</param>
/// <param name="Status">EventStatus name (Draft/Active/UploadClosed/Expired/Deleted); null/unknown = all.</param>
public sealed record AdminEventQuery(string? Search, string? Status, int Page, int PageSize);

/// <summary>One event row for the admin list.</summary>
public sealed record AdminEventRecord(
    Guid Id,
    string Title,
    string Type,
    string Status,
    Guid OwnerUserId,
    string? PackageSlug,
    string? PackageName,
    DateOnly UploadStartDate,
    DateOnly? UploadEndsAt,
    DateOnly? ExpiresAt,
    bool HasCover,
    DateTimeOffset CreatedAt);

/// <summary>A page of events plus the total match count.</summary>
public sealed record AdminEventPage(IReadOnlyList<AdminEventRecord> Items, int Total);

/// <summary>Full event detail for the admin detail view (adds guest token/URL).</summary>
public sealed record AdminEventDetail(
    Guid Id,
    string Title,
    string Type,
    string Status,
    Guid OwnerUserId,
    string? PackageSlug,
    string? PackageName,
    DateOnly UploadStartDate,
    DateOnly? UploadEndsAt,
    DateOnly? ExpiresAt,
    bool HasCover,
    string GuestToken,
    string GuestUrl,
    DateTimeOffset CreatedAt);
