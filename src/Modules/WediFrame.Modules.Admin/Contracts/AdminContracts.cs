namespace WediFrame.Modules.Admin.Contracts;

/// <summary>One row of the audit trail, shaped for the admin UI.</summary>
public sealed record AuditLogItemResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid? ActorUserId,
    string Action,
    string? EntityType,
    string? EntityId,
    string? Details);

/// <summary>One user row for the admin UI (no secrets).</summary>
public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string Role,
    string PreferredLanguage,
    DateTimeOffset CreatedAt);

/// <summary>One event row for the admin list (owner email resolved by the endpoint).</summary>
public sealed record AdminEventResponse(
    Guid Id,
    string Title,
    string Type,
    string Status,
    Guid OwnerUserId,
    string? OwnerEmail,
    string? PackageSlug,
    string? PackageName,
    DateOnly UploadStartDate,
    DateOnly? UploadEndsAt,
    DateOnly? ExpiresAt,
    bool HasCover,
    DateTimeOffset CreatedAt);

/// <summary>Full event detail for the admin detail view (adds guest token/URL).</summary>
public sealed record AdminEventDetailResponse(
    Guid Id,
    string Title,
    string Type,
    string Status,
    Guid OwnerUserId,
    string? OwnerEmail,
    string? PackageSlug,
    string? PackageName,
    DateOnly UploadStartDate,
    DateOnly? UploadEndsAt,
    DateOnly? ExpiresAt,
    bool HasCover,
    string GuestToken,
    string GuestUrl,
    DateTimeOffset CreatedAt);

/// <summary>PATCH body for admin media visibility toggle ("Visible" | "Hidden").</summary>
public sealed record AdminSetVisibilityRequest(string? Visibility);

/// <summary>Generic paged envelope for admin list endpoints.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total);
