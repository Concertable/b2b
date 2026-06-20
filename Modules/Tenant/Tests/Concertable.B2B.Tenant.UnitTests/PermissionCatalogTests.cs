using System.Reflection;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.UnitTests;

/// <summary>
/// Recovers the compile-time guarantee an enum would have given (§1.3): every <see cref="Permissions"/>
/// constant is granted to at least one role, and the catalog grants only declared constants — so a typo'd
/// or orphaned permission string fails the build's tests, not silently 403s in production. Plus spot-checks
/// of the role bundles that pin down behavior.
/// </summary>
public sealed class PermissionCatalogTests
{
    private static readonly IReadOnlySet<string> DeclaredPermissions =
        typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

    [Fact]
    public void EveryDeclaredPermission_IsGrantedToAtLeastOneRole()
    {
        var ungranted = DeclaredPermissions.Except(PermissionCatalog.All).ToList();
        Assert.True(ungranted.Count == 0, $"Permissions declared but absent from the catalog matrix: {string.Join(", ", ungranted)}");
    }

    [Fact]
    public void EveryCatalogPermission_IsADeclaredConstant()
    {
        var unknown = PermissionCatalog.All.Except(DeclaredPermissions).ToList();
        Assert.True(unknown.Count == 0, $"Catalog grants permissions that are not Permissions.* constants: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void Owner_HoldsEveryPermission()
    {
        foreach (var permission in DeclaredPermissions)
            Assert.True(PermissionCatalog.Grants(TenantRole.Owner, permission), $"Owner is missing {permission}");
    }

    [Theory]
    [InlineData(TenantRole.Finance, Permissions.PayoutsManage, true)]
    [InlineData(TenantRole.Finance, Permissions.SettlementTrigger, true)]
    [InlineData(TenantRole.Finance, Permissions.ProfileEdit, false)]
    [InlineData(TenantRole.Manager, Permissions.OpportunitiesManage, true)]
    [InlineData(TenantRole.Manager, Permissions.PayoutsManage, false)]
    [InlineData(TenantRole.Manager, Permissions.TenantDelete, false)]
    [InlineData(TenantRole.Staff, Permissions.MessagesSend, true)]
    [InlineData(TenantRole.Staff, Permissions.ProfileEdit, false)]
    [InlineData(TenantRole.Door, Permissions.ConcertsCheckIn, true)]
    [InlineData(TenantRole.Door, Permissions.ConcertsOpsEdit, false)]
    [InlineData(TenantRole.Sound, Permissions.ConcertsOpsEdit, true)]
    [InlineData(TenantRole.Sound, Permissions.ConcertsCheckIn, false)]
    public void Grants_MatchesMatrix(TenantRole role, string permission, bool expected) =>
        Assert.Equal(expected, PermissionCatalog.Grants(role, permission));

    [Theory]
    [InlineData(TenantRole.Door)]
    [InlineData(TenantRole.Sound)]
    public void ReservedRoles_AlwaysHaveOperationsView(TenantRole role) =>
        Assert.True(PermissionCatalog.Grants(role, Permissions.OperationsView));
}
