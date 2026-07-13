namespace WediFrame.Modules.Events.Contracts;

// Error strings are machine-readable codes ("events.title_required", ...);
// the frontend maps them to localized UI strings.

public sealed record CreateEventRequest(string Title, DateOnly UploadStartDate, string? Type);

public sealed record EventResponse(
    Guid Id,
    string Title,
    string Type,
    DateOnly UploadStartDate,
    string Status,
    string GuestToken,
    string GuestUrl,
    string? CoverPhotoKey,
    DateTimeOffset CreatedAt);
