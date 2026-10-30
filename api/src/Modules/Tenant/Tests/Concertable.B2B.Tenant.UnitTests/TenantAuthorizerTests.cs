using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Infrastructure.Authorization;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantAuthorizerTests
{
    private readonly Mock<IMembershipContext> membership = new();
    private readonly Mock<IMembershipReadRepository> memberships = new();
    private readonly Mock<IRoleRepository> roles = new();
    private readonly MembershipSnapshot owner = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1,
        AuthorizationCatalog.Presets["Owner"].Permissions.ToImmutableDictionary());
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
    public async Task Authorize_UnchangedOwner_ReturnsTheLockedSnapshot()
    {
        var result = await authorizer.AuthorizeAsync(
            owner.TenantId, TenantPermission.MembersManageRoles, ownerRequired: true);

        Assert.True(result.TryGetValue(out var actor));
        Assert.Same(owner, actor);
    }

    [Fact]
    public async Task Authorize_AnotherTenant_IsDeniedWithoutLocking()
    {
        var result = await authorizer.AuthorizeAsync(
            Guid.NewGuid(), TenantPermission.MembersManageRoles, ownerRequired: true);

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
            owner.TenantId, TenantPermission.TenantSettingsEdit, ownerRequired: false);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Authorize_MissingPermission_IsDenied()
    {
        var staff = owner with
        {
            Permissions = AuthorizationCatalog.Presets["Staff"].Permissions.ToImmutableDictionary(),
        };
        membership.SetupGet(context => context.Membership).Returns(staff);
        memberships.Setup(reads => reads.LockIfUnchangedAsync(staff, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var result = await authorizer.AuthorizeAsync(
            owner.TenantId, TenantPermission.MembersInvite, ownerRequired: false);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Authorize_OwnerRequiredWithoutTheProtectedOwnerRole_IsDenied()
    {
        roles.Setup(value => value.HasProtectedOwnerAsync(
                owner.TenantId, owner.MembershipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Assert.True((await authorizer.AuthorizeAsync(
            owner.TenantId, TenantPermission.MembersRemove, ownerRequired: true)).IsFailure);
        Assert.True((await authorizer.AuthorizeAsync(
            owner.TenantId, TenantPermission.MembersInvite, ownerRequired: false)).IsSuccess);
    }
}
