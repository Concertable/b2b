namespace Concertable.B2B.Admin.Contracts;

public interface IAdminModule
{
    /// <summary>Whether the current request's authenticated user holds platform-admin authority — the
    /// <c>/api/auth/me</c> <c>IsAdmin</c> flag.</summary>
    Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default);

    /// <summary>Grants admin for <paramref name="sub"/> if eligible (matching pending invitation, or the
    /// bootstrap email with no admin yet). Called by the User module's registration handler inside its
    /// cross-module unit of work, so this enlists in that ambient transaction — user creation and admin
    /// granting land atomically.</summary>
    Task GrantIfEligibleAsync(Guid sub, string email, CancellationToken ct = default);
}
