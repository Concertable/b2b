namespace Concertable.B2B.Admin.Contracts;

public interface IAdminModule
{
    /// <summary>Whether the current request's authenticated user holds platform-admin authority — the
    /// <c>/api/auth/me</c> <c>IsAdmin</c> flag.</summary>
    Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default);
}
