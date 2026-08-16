namespace WediFrame.Shared.Admin;

/// <summary>
/// Cross-module admin operations on identity users. Implemented by the Identity module
/// and consumed by the Admin module (e.g. startup bootstrap promotion). Admin never
/// references Identity directly — only this Shared contract — so there is no cycle.
/// </summary>
public interface IAdminIdentity
{
    /// <summary>
    /// Promote every EXISTING user whose (normalized) email is in <paramref name="emails"/>
    /// to the Admin role. Idempotent — already-admin users are skipped. Returns the count
    /// of users newly promoted.
    /// </summary>
    Task<int> PromoteEmailsToAdminAsync(IReadOnlyCollection<string> emails, CancellationToken ct);
}
