namespace Concertable.B2B.Admin.Application.Interfaces;

internal interface IAdminProfileRepository
{
    /// <summary>Admins currently provisioned — the last-admin invariant reads this before a revoke.</summary>
    Task<int> CountAdminsAsync(CancellationToken ct = default);

    /// <summary>Every admin's sub — the admin list joins these to email via <c>IUserModule.GetEmailsByIdsAsync</c>.</summary>
    Task<IReadOnlyList<Guid>> ListAdminSubsAsync(CancellationToken ct = default);

    Task<bool> IsAdminAsync(Guid sub, CancellationToken ct = default);

    void GrantAdmin(Guid sub);

    void RemoveAdmin(Guid sub);
}
