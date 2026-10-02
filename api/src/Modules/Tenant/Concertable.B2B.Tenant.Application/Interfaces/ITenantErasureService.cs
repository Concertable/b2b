namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantErasureService
{
    Task<IReadOnlySet<Guid>> SeverMembershipsAsync(Guid userId, IReadOnlySet<Guid> capturedTenantIds, CancellationToken ct = default);

    Task PurgePendingInvitationsAsync(string email, CancellationToken ct = default);
}
