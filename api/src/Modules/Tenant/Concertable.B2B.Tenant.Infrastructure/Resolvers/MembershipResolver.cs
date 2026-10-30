using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Resolvers;

internal sealed class MembershipResolver : IMembershipResolver
{
    private readonly ITenantResolver tenantResolver;

    public MembershipResolver(ITenantResolver tenantResolver)
    {
        this.tenantResolver = tenantResolver;
    }

    public Task<Option<MembershipSnapshot>> ResolveSnapshotAsync(
        MembershipSnapshot expected,
        CancellationToken ct = default) =>
        tenantResolver.ResolveAsync(expected, expected.TenantId, ct: ct)
            .Map(resolution => resolution.Actor);
}
