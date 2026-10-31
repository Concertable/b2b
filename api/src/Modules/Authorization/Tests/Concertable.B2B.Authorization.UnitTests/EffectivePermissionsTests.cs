using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class EffectivePermissionsTests
{
    [Fact]
    public void Indexer_DefaultValue_HoldsNoPermissions()
    {
        Assert.Equal(ResourceAccess.None, default(EffectivePermissions)[TenantPermission.OperationsView]);
        Assert.Empty(default(EffectivePermissions));
    }

    [Fact]
    public void From_SamePermissionFromSeveralRoles_KeepsTheBroadestAccess()
    {
        var permissions = EffectivePermissions.From(
        [
            KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.Assigned),
            KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.All),
            KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.Assigned),
        ]);

        Assert.Equal(ResourceAccess.All, permissions[TenantPermission.OperationsView]);
    }

    [Fact]
    public void Equality_DefaultAndEmpty_AreEqual()
    {
        var empty = EffectivePermissions.From([]);

        Assert.True(default(EffectivePermissions) == empty);
        Assert.Equal(default(EffectivePermissions).GetHashCode(), empty.GetHashCode());
    }

    [Fact]
    public void Equality_SameEntriesInAnotherOrder_AreEqual()
    {
        var first = EffectivePermissions.From(
        [
            KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.All),
            KeyValuePair.Create(TenantPermission.TermsRead, ResourceAccess.Assigned),
        ]);
        var second = EffectivePermissions.From(
        [
            KeyValuePair.Create(TenantPermission.TermsRead, ResourceAccess.Assigned),
            KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.All),
        ]);

        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentAccess_AreNotEqual()
    {
        var all = EffectivePermissions.From(
            [KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.All)]);
        var assigned = EffectivePermissions.From(
            [KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.Assigned)]);

        Assert.True(all != assigned);
    }
}
