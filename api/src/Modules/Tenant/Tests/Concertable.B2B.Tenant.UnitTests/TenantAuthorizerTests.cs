using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Infrastructure.Authorization;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantAuthorizerTests
{
    private readonly Mock<IAuthorizationContext> membership = new();
    private readonly Mock<IMembershipReadRepository> memberships = new();
    private readonly Mock<IRoleRepository> roles = new();
    private readonly MembershipSnapshot owner = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1,
        EffectivePermissions.From(AuthorizationCatalog.Presets["Owner"].Permissions));
    private readonly TenantAuthorizer authorizer;

    public TenantAuthorizerTests()
    {
        membership.SetupGet(context => context.Membership).Returns(owner);
        memberships.Setup(reads => reads.LockIfUnchangedAsync(owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        roles.Setup(value => value.HasProtectedOwnerAsync(
                owner.TenantId, owner.MembershipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        authorizer = new TenantAuthorizer(membership.Object, memberships.Object, roles.Object);
    }

    [Fact]
    public async Task AuthorizeOwner_UnchangedOwner_ReturnsTheLockedSnapshot()
    {
        var result = await authorizer.AuthorizeOwnerAsync(
            owner.TenantId, TenantPermission.MembersManageRoles);

        Assert.True(result.TryGetValue(out var actor));
        Assert.Same(owner, actor);
    }

    [Fact]
    public async Task AuthorizeOwner_AnotherTenant_IsDeniedWithoutLocking()
    {
        var result = await authorizer.AuthorizeOwnerAsync(
            Guid.NewGuid(), TenantPermission.MembersManageRoles);

        Assert.True(result.IsFailure);
        memberships.Verify(reads => reads.LockIfUnchangedAsync(
            It.IsAny<MembershipSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Authorize_AuthorityChangedSinceTheRequestStarted_IsDenied()
    {
        memberships.Setup(reads => reads.LockIfUnchangedAsync(owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipSnapshot?)null);

        var result = await authorizer.AuthorizeAsync(
            owner.TenantId, TenantPermission.TenantSettingsEdit);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Authorize_MissingPermission_IsDenied()
    {
        var staff = owner with
        {
            Permissions = EffectivePermissions.From(AuthorizationCatalog.Presets["Staff"].Permissions),
        };
        membership.SetupGet(context => context.Membership).Returns(staff);
        memberships.Setup(reads => reads.LockIfUnchangedAsync(staff, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var result = await authorizer.AuthorizeAsync(
            owner.TenantId, TenantPermission.MembersInvite);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AuthorizeOwner_WithoutTheProtectedOwnerRole_IsDeniedWhileAuthorizeAllows()
    {
        roles.Setup(value => value.HasProtectedOwnerAsync(
                owner.TenantId, owner.MembershipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Assert.True((await authorizer.AuthorizeOwnerAsync(
            owner.TenantId, TenantPermission.MembersRemove)).IsFailure);
        Assert.True((await authorizer.AuthorizeAsync(
            owner.TenantId, TenantPermission.MembersInvite)).IsSuccess);
    }
}
