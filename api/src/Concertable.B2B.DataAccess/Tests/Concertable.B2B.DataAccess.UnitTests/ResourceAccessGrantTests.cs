using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class ResourceAccessGrantTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    private static readonly MembershipSnapshot Actor = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1,
        ImmutableDictionary<TenantPermission, ResourceCoverage>.Empty
            .Add(TenantPermission.OperationsView, ResourceCoverage.TenantResources)
            .Add(TenantPermission.ConcertsOpsEdit, ResourceCoverage.AssignedResources));

    [Fact]
    public void IsLiveFor_TenantCoverage_CoversTenantWideAndOwnAssignedGrants()
    {
        Assert.True(IsLive(TenantPermission.OperationsView, Grant(membershipId: null)));
        Assert.True(IsLive(TenantPermission.OperationsView, Grant(membershipId: Actor.MembershipId)));
        Assert.False(IsLive(TenantPermission.OperationsView, Grant(membershipId: Guid.NewGuid())));
    }

    [Fact]
    public void IsLiveFor_AssignedCoverage_CoversOnlyOwnAssignedGrants()
    {
        Assert.True(IsLive(TenantPermission.ConcertsOpsEdit, Grant(membershipId: Actor.MembershipId)));
        Assert.False(IsLive(TenantPermission.ConcertsOpsEdit, Grant(membershipId: null)));
    }

    [Fact]
    public void IsLiveFor_PermissionNotHeld_CoversNothing()
    {
        Assert.False(IsLive(TenantPermission.SettlementView, Grant(membershipId: null)));
    }

    [Fact]
    public void IsLiveFor_GrantOutsideItsValidityOrRevoked_IsNotLive()
    {
        Assert.False(IsLive(TenantPermission.OperationsView, Grant(validFrom: Now.AddMinutes(1))));
        Assert.False(IsLive(TenantPermission.OperationsView, Grant(validUntil: Now)));
        Assert.True(IsLive(TenantPermission.OperationsView, Grant(validUntil: Now.AddTicks(1))));

        var revoked = Grant();
        revoked.Revoke(Now);
        Assert.False(IsLive(TenantPermission.OperationsView, revoked));
    }

    [Fact]
    public void IsLiveFor_AnotherTenantOrScope_IsNotLive()
    {
        Assert.False(IsLive(TenantPermission.OperationsView, Grant(tenantId: Guid.NewGuid())));
        Assert.False(IsLive(TenantPermission.OperationsView, Grant(scope: TestScope.Finance)));
    }

    private static bool IsLive(TenantPermission permission, TestGrant grant) =>
        new[] { grant }.AsQueryable().Any(
            ResourceAccessGrant<TestScope>.IsLiveFor<TestGrant>(Actor, permission, Now, TestScope.Operations));

    private static TestGrant Grant(
        Guid? membershipId = null,
        Guid? tenantId = null,
        TestScope scope = TestScope.Operations,
        DateTime? validFrom = null,
        DateTime? validUntil = null) =>
        new(tenantId ?? Actor.TenantId, membershipId, scope, validFrom ?? Now.AddHours(-1), validUntil);

    public enum TestScope
    {
        Operations = 1,
        Finance,
    }

    public sealed class TestGrant : ResourceAccessGrant<TestScope>
    {
        public TestGrant(Guid tenantId, Guid? membershipId, TestScope scope, DateTime validFrom, DateTime? validUntil) =>
            Initialize(1, tenantId, membershipId, scope, Guid.NewGuid(), null,
                ResourceGrantKind.MemberAssignment, validFrom, validUntil);

        public void Revoke(DateTime at) => RevokeCore(at);
    }

    [Fact]
    public void IsLiveFor_SeveralScopes_MatchesAnyOfThem()
    {
        var live = ResourceAccessGrant<TestScope>.IsLiveFor<TestGrant>(
            Actor, TenantPermission.OperationsView, Now, TestScope.Operations, TestScope.Finance);

        Assert.True(new[] { Grant(scope: TestScope.Finance) }.AsQueryable().Any(live));
    }

    [Fact]
    public void IsLiveFor_NoScopes_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            ResourceAccessGrant<TestScope>.IsLiveFor<TestGrant>(Actor, TenantPermission.OperationsView, Now));
    }

    [Fact]
    public void IsLiveFor_GrantStartingNow_IsLive()
    {
        Assert.True(IsLive(TenantPermission.OperationsView, Grant(validFrom: Now)));
    }
}
