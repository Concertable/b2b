using System.Reflection;
using Concertable.B2B.Authorization.Infrastructure.Authorization;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class PermissionCatalogTests
{
    private readonly PermissionCatalog catalog;

    public PermissionCatalogTests()
    {
        this.catalog = new PermissionCatalog();
    }

    private static IReadOnlySet<string> DeclaredPermissions =>
        typeof(TenantPermission)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

    [Fact]
    public void Grants_DeclaredConstants_ExactlyMatchTheGrantedSet()
    {
        var declared = DeclaredPermissions;

        Assert.Empty(declared.Except(PermissionCatalog.All));
        Assert.Empty(PermissionCatalog.All.Except(declared));
    }

    [Fact]
    public void Grants_Owner_HoldsEveryPermission()
    {
        var ungranted = DeclaredPermissions
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
    [InlineData(TenantRole.Finance, TenantPermission.PayoutsManage, true)]
    [InlineData(TenantRole.Finance, TenantPermission.SettlementTrigger, true)]
    [InlineData(TenantRole.Finance, TenantPermission.ProfileEdit, false)]
    [InlineData(TenantRole.Manager, TenantPermission.OpportunitiesManage, true)]
    [InlineData(TenantRole.Manager, TenantPermission.ResourcesShare, true)]
    [InlineData(TenantRole.Manager, TenantPermission.PayoutsManage, false)]
    [InlineData(TenantRole.Manager, TenantPermission.TenantDelete, false)]
    [InlineData(TenantRole.Manager, TenantPermission.MembersManageRoles, false)]
    [InlineData(TenantRole.Staff, TenantPermission.MessagesSend, true)]
    [InlineData(TenantRole.Staff, TenantPermission.ProfileEdit, false)]
    [InlineData(TenantRole.Door, TenantPermission.ConcertsCheckIn, true)]
    [InlineData(TenantRole.Door, TenantPermission.ConcertsOpsEdit, false)]
    [InlineData(TenantRole.Sound, TenantPermission.ConcertsOpsEdit, true)]
    [InlineData(TenantRole.Sound, TenantPermission.ConcertsCheckIn, false)]
    [InlineData(TenantRole.RestrictedParticipant, TenantPermission.MessagesRead, false)]
    [InlineData(TenantRole.RestrictedParticipant, TenantPermission.SettlementView, false)]
    [InlineData(TenantRole.RestrictedParticipant, TenantPermission.ResourcesShare, false)]
    public void Grants_Role_MatchesTheMatrix(TenantRole role, string permission, bool expected) =>
        Assert.Equal(expected, this.catalog.Grants(role, permission));

    [Theory]
    [InlineData(TenantPermission.ApplicationsSubmit)]
    [InlineData(TenantPermission.ApplicationsDecide)]
    [InlineData(TenantPermission.OpportunitiesManage)]
    public void Grants_Manager_ReachesBothSidesOfTheMarketplace(string permission) =>
        Assert.True(this.catalog.Grants(TenantRole.Manager, permission));
}
