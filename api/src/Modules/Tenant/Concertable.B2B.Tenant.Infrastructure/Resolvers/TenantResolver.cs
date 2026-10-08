using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Resolvers;

internal sealed class TenantResolver : ITenantResolver
{
    private readonly ITenantRepository tenantRepository;
    private readonly IMembershipRepository membershipRepository;

    public TenantResolver(ITenantRepository tenantRepository, IMembershipRepository membershipRepository)
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
        var actor = memberships.SingleOrDefault(membership => membership == expectedActor);
        if (actor is null)
            return null;

        var targetMembership = memberships.SingleOrDefault(membership =>
            membership.MembershipId == targetMembershipId && membership.TenantId == targetTenantId);
        return new TenantResolution(actor, existingTenantIds.Contains(targetTenantId), targetMembership);
    }

    public async Task<Option<TenantSetResolution>> ResolveManyAsync(
        MembershipSnapshot expectedActor,
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default)
    {
        var existingTenantIds = await tenantRepository.GetExistingIdsForShareAsync(tenantIds, ct);
        var memberships = await membershipRepository.GetSnapshotsByIdsForShareAsync(
            [expectedActor.MembershipId], ct);
        var actor = memberships.SingleOrDefault(membership => membership == expectedActor);
        return actor is null ? null : new TenantSetResolution(actor, existingTenantIds);
    }
}
