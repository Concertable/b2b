using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class MembershipSnapshotTests
{
    private readonly MembershipSnapshot actor = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Guid.Parse("33333333-3333-3333-3333-333333333333"), 3, 5,
        Permissions((TenantPermission.OperationsView, ResourceAccess.All)));

    [Theory]
    [InlineData(ResourceAccess.Assigned)]
    [InlineData(ResourceAccess.All)]
    public void HasPermission_ResourceReadPermission_AllowsEitherGrantedAccess(ResourceAccess access)
    {
        var reader = actor with
        {
            Permissions = Permissions(
                (TenantPermission.OperationsView, ResourceAccess.All), (TenantPermission.TermsRead, access)),
        };

        Assert.True(reader.HasPermission(TenantPermission.TermsRead));
    }

    [Theory]
    [InlineData(ResourceAccess.Assigned, false)]
    [InlineData(ResourceAccess.All, true)]
    [InlineData(ResourceAccess.None, false)]
    public void Permissions_CreationPermission_RequiresAllAccess(ResourceAccess access, bool expected)
    {
        var creator = actor with { Permissions = Permissions((TenantPermission.ApplicationsSubmit, access)) };

        Assert.Equal(expected, creator.Permissions[TenantPermission.ApplicationsSubmit].Includes(ResourceAccess.All));
    }

    [Fact]
    public void Permissions_UnheldOrDefaultPermission_IsNone()
    {
        Assert.Equal(ResourceAccess.None, actor.Permissions[TenantPermission.SettlementView]);
        Assert.Equal(ResourceAccess.None, actor.Permissions[default]);
        Assert.False(actor.HasPermission(TenantPermission.SettlementView));
    }

    [Fact]
    public void Equality_PermissionAddedUnderANewVersion_IsAnotherAuthority()
    {
        var changed = actor with
        {
            PermissionVersion = actor.PermissionVersion + 1,
            Permissions = Permissions(
                (TenantPermission.OperationsView, ResourceAccess.All),
                (TenantPermission.ApplicationsSubmit, ResourceAccess.All)),
        };

        Assert.NotEqual(actor, changed);
        Assert.Equal(actor, actor with { });
    }

    [Fact]
    public void Equality_RolePolicyVersionChanged_IsAnotherAuthority()
    {
        Assert.NotEqual(actor, actor with { RolePolicyVersion = actor.RolePolicyVersion + 1 });
    }

    [Fact]
    public void Equality_AccessNarrowedUnderTheSameVersions_IsAnotherAuthority()
    {
        var narrowed = actor with
        {
            Permissions = Permissions((TenantPermission.OperationsView, ResourceAccess.Assigned)),
        };

        Assert.NotEqual(actor, narrowed);
    }

    [Fact]
    public void Equality_AnotherMembershipOrNone_IsAnotherAuthority()
    {
        Assert.NotEqual(actor, actor with { MembershipId = Guid.NewGuid() });
        Assert.False(actor == null);
    }

    [Fact]
    public void Equality_IndependentlyBuiltEqualPermissions_IsTheSameAuthority()
    {
        var rebuilt = actor with { Permissions = Permissions((TenantPermission.OperationsView, ResourceAccess.All)) };

        Assert.Equal(actor, rebuilt);
        Assert.True(actor == rebuilt);
    }

    private static EffectivePermissions Permissions(
        params (TenantPermission Permission, ResourceAccess Access)[] grants) =>
        EffectivePermissions.From(grants.Select(grant => KeyValuePair.Create(grant.Permission, grant.Access)));
}
