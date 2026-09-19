using Concertable.B2B.Authorization.Infrastructure.Authorization;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class PermissionCatalogTests
{
    public static TheoryData<TenantRole, TenantPermission, bool> RolePermissions => new()
    {
        { TenantRole.Finance, TenantPermission.PayoutsManage, true },
        { TenantRole.Finance, TenantPermission.SettlementTrigger, true },
        { TenantRole.Finance, TenantPermission.ProfileEdit, false },
        { TenantRole.Manager, TenantPermission.OpportunitiesManage, true },
        { TenantRole.Manager, TenantPermission.ResourcesShare, true },
        { TenantRole.Manager, TenantPermission.PayoutsManage, false },
        { TenantRole.Manager, TenantPermission.TenantDelete, false },
        { TenantRole.Manager, TenantPermission.MembersManageRoles, false },
        { TenantRole.Staff, TenantPermission.MessagesSend, true },
        { TenantRole.Staff, TenantPermission.ProfileEdit, false },
        { TenantRole.Door, TenantPermission.ConcertsCheckIn, true },
        { TenantRole.Door, TenantPermission.ConcertsOpsEdit, false },
        { TenantRole.Sound, TenantPermission.ConcertsOpsEdit, true },
        { TenantRole.Sound, TenantPermission.ConcertsCheckIn, false },
    };

    public static TheoryData<TenantPermission> ManagerMarketplacePermissions => new()
    {
        TenantPermission.ApplicationsSubmit,
        TenantPermission.ApplicationsDecide,
        TenantPermission.OpportunitiesManage,
    };

    private readonly PermissionCatalog catalog;

    public PermissionCatalogTests()
    {
        this.catalog = new PermissionCatalog();
    }

    [Fact]
    public void All_DeclaredPermissions_ExactlyMatchTheGrantedSet()
    {
        Assert.Empty(TenantPermission.All.Except(PermissionCatalog.All));
        Assert.Empty(PermissionCatalog.All.Except(TenantPermission.All));
    }

    [Fact]
    public void Grants_Owner_HoldsEveryPermission()
    {
        var ungranted = TenantPermission.All
            .Where(permission => !this.catalog.Grants(TenantRole.Owner, permission))
            .ToList();

        Assert.Empty(ungranted);
    }

    [Fact]
    public void Grants_EveryRole_CanSeeOperations()
    {
        var blind = Enum.GetValues<TenantRole>()
            .Where(role => !this.catalog.Grants(role, TenantPermission.OperationsView))
            .ToList();

        Assert.Empty(blind);
    }

    [Theory]
    [MemberData(nameof(RolePermissions))]
    public void Grants_Role_MatchesTheMatrix(TenantRole role, TenantPermission permission, bool expected) =>
        Assert.Equal(expected, this.catalog.Grants(role, permission));

    [Theory]
    [MemberData(nameof(ManagerMarketplacePermissions))]
    public void Grants_Manager_ReachesBothSidesOfTheMarketplace(TenantPermission permission) =>
        Assert.True(this.catalog.Grants(TenantRole.Manager, permission));
}
