using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Lockers;

internal sealed class MembershipLocker : IMembershipLocker
{
    private readonly IMembershipRepository membershipRepository;

    public MembershipLocker(IMembershipRepository membershipRepository)
    {
        this.membershipRepository = membershipRepository;
    }

    public async Task<Option<LockedMemberships>> LockAsync(
        MembershipSnapshot expectedActor,
        Guid targetTenantId,
        Guid? targetMembershipId = null,
        CancellationToken ct = default)
    {
        Guid[] membershipIds = targetMembershipId is { } targetId
            ? [expectedActor.MembershipId, targetId]
            : [expectedActor.MembershipId];
        var memberships = await membershipRepository.GetSnapshotsByIdsForShareAsync(membershipIds, ct);
        var actor = memberships.SingleOrDefault(membership => membership == expectedActor);
        if (actor is null)
            return null;

        var target = memberships.SingleOrDefault(membership =>
            membership.MembershipId == targetMembershipId && membership.TenantId == targetTenantId);
        return new LockedMemberships(actor, target);
    }
}
