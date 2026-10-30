using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Resolvers;

internal sealed class TargetTenantResolver : ITargetTenantResolver
{
    private readonly ITenantRepository tenantRepository;
    private readonly IMembershipRepository membershipRepository;

    public TargetTenantResolver(ITenantRepository tenantRepository, IMembershipRepository membershipRepository)
    {
        this.tenantRepository = tenantRepository;
        this.membershipRepository = membershipRepository;
    }

    public async Task<Option<TenantResolution>> ResolveAsync(
        MembershipSnapshot expectedActor,
        Guid targetTenantId,
        Guid? targetMembershipId = null,
        CancellationToken ct = default)
    {
        var existingTenantIds = await tenantRepository.GetExistingIdsForShareAsync(
            [expectedActor.TenantId, targetTenantId], ct);
        Guid[] membershipIds = targetMembershipId is { } targetId
            ? [expectedActor.MembershipId, targetId]
            : [expectedActor.MembershipId];
        var memberships = await membershipRepository.GetSnapshotsByIdsForShareAsync(membershipIds, ct);
        var actor = memberships.SingleOrDefault(membership => membership.HasSameAuthorityAs(expectedActor));
        if (actor is null)
            return null;

        var targetMembership = memberships.SingleOrDefault(membership =>
            membership.MembershipId == targetMembershipId && membership.TenantId == targetTenantId);
        return new TenantResolution(actor, existingTenantIds.Contains(targetTenantId), targetMembership);
    }

    public async Task<IReadOnlySet<Guid>> ResolveManyAsync(
        MembershipSnapshot expectedActor,
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default)
    {
        var requestedTenantIds = tenantIds.Distinct().ToHashSet();
        var lockSet = requestedTenantIds.Append(expectedActor.TenantId).Order().ToArray();
        var existingTenantIds = await tenantRepository.GetExistingIdsForShareAsync(lockSet, ct);
        return existingTenantIds.Where(requestedTenantIds.Contains).ToHashSet();
    }
}
