namespace WediFrame.Shared.Directory;

/// <summary>A host's contact details, resolved by user id.</summary>
public sealed record UserContact(Guid UserId, string Email, string Language);

/// <summary>
/// Cross-module port for reading host contact info (email + language) by user id.
/// Identity owns the User table and implements this; other modules (e.g. the
/// retention reminder in Retention) depend on the Shared port, never on Identity
/// — same one-way pattern as <see cref="Media.IEventMediaPurge"/>.
/// </summary>
public interface IUserDirectory
{
    /// <summary>Single contact, or null if the user no longer exists.</summary>
    Task<UserContact?> GetContactAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Batch lookup for background jobs. Returns only the ids that resolved;
    /// missing users are simply absent from the map.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, UserContact>> GetContactsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
}
