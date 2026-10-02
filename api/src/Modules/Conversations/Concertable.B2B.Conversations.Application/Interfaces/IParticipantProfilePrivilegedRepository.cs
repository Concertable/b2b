namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IParticipantProfilePrivilegedRepository
{
    Task<IReadOnlyList<ParticipantProfile>> ListByTenantIdsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
