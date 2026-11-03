using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class PermissionSetTests
{
    [Fact]
    public void Indexer_DefaultValue_HoldsNoPermissions()
    {
        Assert.Equal(ResourceAccess.None, default(PermissionSet)[TenantPermission.OperationsView]);
        Assert.Empty(default(PermissionSet));
    }

    [Fact]
    public void From_SamePermissionFromSeveralRoles_KeepsTheBroadestAccess()
    {
        var permissions = PermissionSet.From(
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
        var empty = PermissionSet.From([]);

        Assert.True(default(PermissionSet) == empty);
        Assert.Equal(default(PermissionSet).GetHashCode(), empty.GetHashCode());
    }

    [Fact]
    public void Equality_SameEntriesInAnotherOrder_AreEqual()
    {
        var first = PermissionSet.From(
        [
            KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.All),
            KeyValuePair.Create(TenantPermission.TermsRead, ResourceAccess.Assigned),
        ]);
        var second = PermissionSet.From(
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
        var all = PermissionSet.From(
            [KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.All)]);
        var assigned = PermissionSet.From(
            [KeyValuePair.Create(TenantPermission.OperationsView, ResourceAccess.Assigned)]);

        Assert.True(all != assigned);
    }
}
