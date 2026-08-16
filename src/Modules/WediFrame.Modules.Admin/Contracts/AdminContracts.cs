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

/// <summary>Generic paged envelope for admin list endpoints.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total);
