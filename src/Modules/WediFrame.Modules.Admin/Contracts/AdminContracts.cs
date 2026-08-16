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

/// <summary>Generic paged envelope for admin list endpoints.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total);
