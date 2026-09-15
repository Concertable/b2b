namespace Concertable.B2B.Conversations.Application.Interfaces;

/// <summary>Participant-profile read models through the unfiltered platform-admin stance — GDPR erasure
/// pseudonymises the display identity of a wound-down (member-less) tenant, which no ambient tenant is party
/// to. <see cref="ParticipantProfile"/> is keyed by tenant id, not an entity id, so it has no generic repository.</summary>
internal interface IParticipantProfilePrivilegedRepository
{
    Task<IReadOnlyList<ParticipantProfile>> ListByTenantIdsAsync(IReadOnlyList<Guid> tenantIds, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
