using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class RoleServiceTests
{
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly Mock<IRoleRepository> roles = new();
    private readonly Mock<IMembershipRepository> memberships = new();
    private readonly TenantEntity tenant;
    private readonly RoleService service;

    public RoleServiceTests()
    {
        tenant = TenantEntity.Create("Acme Ltd", "contact@acme.test", Guid.NewGuid(), DateTime.UtcNow, tenantId);
        var tenants = new Mock<ITenantRepository>();
        tenants.Setup(value => value.GetByIdWithUpdateLockAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(value => value.TenantId).Returns(tenantId);
        var actor = new MembershipSnapshot(Guid.NewGuid(), tenantId, Guid.NewGuid(), 1, 1,
            PermissionSet.From(SystemRole.Owner.Grants));
        var authorization = new Mock<IAuthorizationContext>();
        authorization.SetupGet(value => value.Membership).Returns(actor);
        roles.Setup(value => value.HasProtectedOwnerAsync(tenantId, actor.MembershipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        service = new RoleService(roles.Object, tenants.Object, memberships.Object, tenantContext.Object,
            authorization.Object, Mock.Of<IInvitationRepository>(), new ImmediateUnitOfWorkBehavior(), TimeProvider.System);
    }

    [Theory]
    [InlineData("Door")]
    [InlineData(" sound ")]
    public async Task CreateAsync_DeferredSystemRoleName_ReturnsNameInUse(string name)
    {
        var result = await service.CreateAsync(new CreateRoleRequest(name, false, []));

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<CreateRoleError.NameInUse>(error);
        roles.Verify(value => value.Insert(It.IsAny<TenantRoleDefinition>()), Times.Never);
    }

    [Theory]
    [InlineData("door")]
    [InlineData(" Sound ")]
    public async Task UpdateAsync_DeferredSystemRoleName_PreservesCustomRole(string name)
    {
        var role = TenantRoleDefinition.CreateCustom(tenantId, "Custom", false,
            new Dictionary<TenantPermission, ResourceAccess>());
        roles.Setup(value => value.GetActiveByIdAsync(tenantId, role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        var result = await service.UpdateAsync(role.Id, new UpdateRoleRequest(name, role.Version, false, []));

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<UpdateRoleError.NameInUse>(error);
        Assert.Equal("Custom", role.Name);
    }

    [Theory]
    [InlineData(SystemRole.Door)]
    [InlineData(SystemRole.Sound)]
    public async Task RetireAsync_ReplacementRequiresRetiredActivity_PreservesMembership(SystemRole replacementRole)
    {
        var now = DateTime.UtcNow;
        tenant.ActivateBusinessActivity(TenantBusinessActivityKind.VenueOperator, now);
        var replacement = TenantRoleDefinition.CreateSystemRole(tenantId, replacementRole);
        tenant.RetireBusinessActivity(TenantBusinessActivityKind.VenueOperator, now.AddSeconds(1));
        var role = TenantRoleDefinition.CreateCustom(tenantId, "Custom", false,
            new Dictionary<TenantPermission, ResourceAccess>());
        var member = TenantMembershipEntity.Create(tenantId, Guid.NewGuid(), [role.Id], null, now);
        roles.Setup(value => value.GetActiveByIdAsync(tenantId, role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        roles.Setup(value => value.GetActiveByIdAsync(tenantId, replacement.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(replacement);
        memberships.Setup(value => value.ListAssignedToRoleAsync(tenantId, role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([member]);

        var result = await service.RetireAsync(role.Id, new RetireRoleRequest(role.Version, replacement.Id));

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RetireRoleError.InvalidReplacement>(error);
        Assert.Equal(role.Id, Assert.Single(member.Assignments).RoleId);
        Assert.Null(role.RetiredAt);
    }
}
