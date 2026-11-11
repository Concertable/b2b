using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class SystemRolesTests
{
    public static TheoryData<SystemRole, TenantPermission, bool> RolePermissions => new()
    {
        { SystemRole.Finance, TenantPermission.PayoutsManage, true },
        { SystemRole.Finance, TenantPermission.SettlementTrigger, true },
        { SystemRole.Finance, TenantPermission.ProfileEdit, false },
        { SystemRole.Manager, TenantPermission.OpportunitiesManage, true },
        { SystemRole.Manager, TenantPermission.ResourcesShare, true },
        { SystemRole.Manager, TenantPermission.PayoutsManage, false },
        { SystemRole.Manager, TenantPermission.TenantDelete, false },
        { SystemRole.Manager, TenantPermission.MembersManageRoles, false },
        { SystemRole.Staff, TenantPermission.MessagesSend, true },
        { SystemRole.Staff, TenantPermission.ProfileEdit, false },
        { SystemRole.Door, TenantPermission.ConcertsCheckIn, true },
        { SystemRole.Door, TenantPermission.ConcertsOpsEdit, false },
        { SystemRole.Sound, TenantPermission.ConcertsOpsEdit, true },
        { SystemRole.Sound, TenantPermission.ConcertsCheckIn, false },
    };

    public static TheoryData<TenantPermission> ManagerMarketplacePermissions => new()
    {
        TenantPermission.ApplicationsSubmit,
        TenantPermission.ApplicationsDecide,
        TenantPermission.OpportunitiesManage,
    };

    [Fact]
    public void OwnerRole_ContainsEveryPermission()
    {
        Assert.True(SystemRole.Owner.IsProtected);
        Assert.Equal(TenantPermission.All.Count, SystemRole.Owner.Grants.Count);
        Assert.All(TenantPermission.All, permission => Assert.True(SystemRole.Owner.Grants.ContainsKey(permission)));
    }

    [Fact]
    public void EveryRole_DeclaresOperationsViewGrant()
    {
        Assert.All(SystemRoles.All, role => Assert.True(role.Grants.ContainsKey(TenantPermission.OperationsView)));
    }

    [Theory]
    [MemberData(nameof(RolePermissions))]
    public void Role_DeclaresExpectedPermission(SystemRole role, TenantPermission permission, bool expected)
    {
        var granted = role.Grants.ContainsKey(permission);

        Assert.Equal(expected, granted);
    }

    [Theory]
    [MemberData(nameof(ManagerMarketplacePermissions))]
    public void ManagerRole_ReachesBothSidesOfMarketplace(TenantPermission permission) =>
        Assert.True(SystemRole.Manager.Grants.ContainsKey(permission));

    [Fact]
    public void DoorAndSound_RequireVenueOperatorActivity()
    {
        Assert.Equal(TenantBusinessActivityKind.VenueOperator, SystemRole.Door.RequiredActivity);
        Assert.Equal(TenantBusinessActivityKind.VenueOperator, SystemRole.Sound.RequiredActivity);
        Assert.Null(SystemRole.Owner.RequiredActivity);
        Assert.Null(SystemRole.Manager.RequiredActivity);
        Assert.Null(SystemRole.Finance.RequiredActivity);
        Assert.Null(SystemRole.Staff.RequiredActivity);
    }

    [Fact]
    public void AvailableTo_WithoutVenueOperatorActivity_ExcludesDoorAndSound()
    {
        var available = SystemRoles.AvailableTo([TenantBusinessActivityKind.Artist]);

        Assert.DoesNotContain(SystemRole.Door, available);
        Assert.DoesNotContain(SystemRole.Sound, available);
        Assert.Contains(SystemRole.Owner, available);
    }

    [Fact]
    public void AvailableTo_WithVenueOperatorActivity_IncludesEveryRole()
    {
        var available = SystemRoles.AvailableTo([TenantBusinessActivityKind.VenueOperator]);

        Assert.Equal(SystemRoles.All.Count, available.Count);
    }
}
