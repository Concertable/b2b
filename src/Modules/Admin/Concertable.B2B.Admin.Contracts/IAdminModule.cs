namespace Concertable.B2B.Admin.Contracts;

public interface IAdminModule
{
    /// <summary>Whether the current request's authenticated user holds platform-admin authority — the
    /// <c>/api/auth/me</c> <c>IsAdmin</c> flag.</summary>
    Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default);

    /// <summary>Grants the current request's authenticated user admin if eligible (matching pending
    /// invitation, or the bootstrap email with no admin yet), then returns whether they're an admin
    /// afterward. Called from <c>UserController.Me()</c> — the first authenticated request after login,
    /// which Auth's own login gate guarantees runs only for a verified mailbox. Deliberately not
    /// registration-time: <c>CredentialRegisteredEvent</c> fires before email verification, so granting
    /// off it would be gate-able by an unverified mailbox.</summary>
    Task<bool> EnsureCurrentUserAdminGrantedIfEligibleAsync(CancellationToken ct = default);
}
