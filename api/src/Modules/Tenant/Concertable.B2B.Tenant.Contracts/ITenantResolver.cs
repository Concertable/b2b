using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantResolver
{
    Task<Option<TenantResolution>> ResolveAsync(
        MembershipSnapshot expectedActor,
        Guid targetTenantId,
        Guid? targetMembershipId = null,
        CancellationToken ct = default);

    Task<IReadOnlySet<Guid>> ResolveManyAsync(
        MembershipSnapshot expectedActor,
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default);
}
