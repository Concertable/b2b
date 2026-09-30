using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

internal sealed class MembershipAuthorityFence : IMembershipAuthorityFence
{
    private readonly ITenantResolver tenantResolver;

    public MembershipAuthorityFence(ITenantResolver tenantResolver)
    {
        this.tenantResolver = tenantResolver;
    }

    public async Task<MembershipSnapshot?> RequireCurrentAsync(
        MembershipSnapshot expected,
        CancellationToken ct = default) =>
        (await tenantResolver.ResolveAsync(expected, expected.TenantId, ct: ct))?.Actor;
}
