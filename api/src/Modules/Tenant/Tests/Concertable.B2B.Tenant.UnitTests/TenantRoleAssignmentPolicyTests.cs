using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantRoleAssignmentPolicyTests
{
    [Theory]
    [InlineData(TenantRole.Owner, TenantRole.Owner, true)]
    [InlineData(TenantRole.Owner, TenantRole.Manager, true)]
    [InlineData(TenantRole.Manager, TenantRole.Staff, true)]
    [InlineData(TenantRole.Manager, TenantRole.Door, true)]
    [InlineData(TenantRole.Manager, TenantRole.Sound, true)]
    [InlineData(TenantRole.Manager, TenantRole.Manager, false)]
    [InlineData(TenantRole.Manager, TenantRole.Owner, false)]
    [InlineData(TenantRole.Manager, TenantRole.Finance, false)]
    [InlineData(TenantRole.Staff, TenantRole.Staff, false)]
    public void CanAssignRole_ActorAndTarget_ReturnsExpected(
        TenantRole actor,
        TenantRole target,
        bool expected)
    {
        var result = TenantRoleAssignmentPolicy.CanAssignRole(actor, target);

        Assert.Equal(expected, result);
    }
}
