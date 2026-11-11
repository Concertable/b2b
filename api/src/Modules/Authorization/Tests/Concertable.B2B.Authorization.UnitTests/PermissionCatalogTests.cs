using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class PermissionCatalogTests
{
    public static TheoryData<string, TenantPermission, bool> PresetPermissions => new()
    {
        { "Finance", TenantPermission.PayoutsManage, true },
        { "Finance", TenantPermission.SettlementTrigger, true },
        { "Finance", TenantPermission.ProfileEdit, false },
        { "Manager", TenantPermission.OpportunitiesManage, true },
        { "Manager", TenantPermission.ResourcesShare, true },
        { "Manager", TenantPermission.PayoutsManage, false },
        { "Manager", TenantPermission.TenantDelete, false },
        { "Manager", TenantPermission.MembersManageRoles, false },
        { "Staff", TenantPermission.MessagesSend, true },
        { "Staff", TenantPermission.ProfileEdit, false },
        { "Door", TenantPermission.ConcertsCheckIn, true },
        { "Door", TenantPermission.ConcertsOpsEdit, false },
        { "Sound", TenantPermission.ConcertsOpsEdit, true },
        { "Sound", TenantPermission.ConcertsCheckIn, false },
    };

    public static TheoryData<TenantPermission> ManagerMarketplacePermissions => new()
    {
        TenantPermission.ApplicationsSubmit,
        TenantPermission.ApplicationsDecide,
        TenantPermission.OpportunitiesManage,
    };

    [Fact]
    public void Permissions_ExactlyMatchDeclaredPermissions()
    {
        Assert.Equal(TenantPermission.All.OrderBy(value => value.Value),
            AuthorizationCatalog.Permissions.Keys.OrderBy(value => value.Value));
    }

    [Fact]
    public void OwnerPreset_ContainsEveryPermission()
    {
        var owner = AuthorizationCatalog.Presets["Owner"];

        Assert.True(owner.IsProtectedOwner);
        Assert.Equal(TenantPermission.All.Count, owner.Permissions.Count);
        Assert.All(TenantPermission.All, permission => Assert.True(owner.Permissions.ContainsKey(permission)));
    }

    [Fact]
    public void Presets_DeclareOperationsViewMetadata()
    {
        Assert.All(AuthorizationCatalog.Presets.Values, preset =>
            Assert.True(preset.Permissions.ContainsKey(TenantPermission.OperationsView)));
    }

    [Theory]
    [MemberData(nameof(PresetPermissions))]
    public void Preset_DeclaresExpectedPermission(string presetKey, TenantPermission permission, bool expected)
    {
        var granted = AuthorizationCatalog.Presets[presetKey].Permissions.ContainsKey(permission);

        Assert.Equal(expected, granted);
    }

    [Theory]
    [MemberData(nameof(ManagerMarketplacePermissions))]
    public void ManagerPreset_ReachesBothSidesOfMarketplace(TenantPermission permission) =>
        Assert.True(AuthorizationCatalog.Presets["Manager"].Permissions.ContainsKey(permission));
}
