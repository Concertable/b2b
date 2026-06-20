using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantContextTests
{
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<IHttpContextAccessor> httpContextAccessor = new();
    private readonly Mock<ITenantRepository> repository = new();

    private TenantContext CreateContext() =>
        new(currentUser.Object, httpContextAccessor.Object, repository.Object);

    private void WithHttpRequest() =>
        httpContextAccessor.SetupGet(h => h.HttpContext).Returns(new Mock<HttpContext>().Object);

    private void WithoutHttpRequest() =>
        httpContextAccessor.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

    /// <summary>Resolve a context backed by a single membership of the given role + persona.</summary>
    private async Task<IMembershipContext> ResolvedMembership(TenantRole role, TenantType type)
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetActiveMembershipAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveMembership(Guid.NewGuid(), role, type));

        var context = CreateContext();
        await context.ResolveAsync();
        return context;
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithMembership_ResolvesTenantAndRole()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetActiveMembershipAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveMembership(tenantId, TenantRole.Owner, TenantType.Venue));

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.Equal(tenantId, ctx.TenantId);
        Assert.True(ctx.HasTenant);
        Assert.False(ctx.IsHost);
        Assert.Equal(TenantRole.Owner, ((IMembershipContext)context).Role);
    }

    [Fact]
    public async Task ResolveAsync_NoHttpRequest_IsHostAndResolvesNothing()
    {
        WithoutHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(Guid.NewGuid());

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.True(ctx.IsHost);
        Assert.Null(ctx.TenantId);
        Assert.False(ctx.HasTenant);
        repository.Verify(
            r => r.GetActiveMembershipAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_AnonymousRequest_NotHostAndFailsClosed()
    {
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns((Guid?)null);

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.False(ctx.IsHost);
        Assert.Null(ctx.TenantId);
        Assert.False(ctx.HasTenant);
        repository.Verify(
            r => r.GetActiveMembershipAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithoutMembership_FailsClosedAndRoleIsNull()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetActiveMembershipAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveMembership?)null);

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.False(ctx.IsHost);
        Assert.Null(ctx.TenantId);
        Assert.False(ctx.HasTenant);

        var membership = (IMembershipContext)context;
        Assert.Null(membership.Role);
        Assert.False(membership.HasPermission(Permissions.OperationsView));
    }

    [Fact]
    public async Task HasPermission_OwnerOfVenue_GrantsVenueScopedPermission()
    {
        var membership = await ResolvedMembership(TenantRole.Owner, TenantType.Venue);

        Assert.True(membership.HasPermission(Permissions.ProfileEdit, TenantType.Venue));
        Assert.True(membership.HasPermission(Permissions.OpportunitiesManage, TenantType.Venue));
    }

    [Fact]
    public async Task HasPermission_WrongPersona_Denies_RightPersona_Grants()
    {
        var membership = await ResolvedMembership(TenantRole.Owner, TenantType.Artist);

        // Owner holds the permission in its bundle, but the active tenant's persona must match the call-site.
        Assert.False(membership.HasPermission(Permissions.ProfileEdit, TenantType.Venue));
        Assert.True(membership.HasPermission(Permissions.ProfileEdit, TenantType.Artist));
        Assert.True(membership.HasPermission(Permissions.ApplicationsSubmit, TenantType.Artist));
        Assert.False(membership.HasPermission(Permissions.ApplicationsDecide, TenantType.Venue));
    }

    [Fact]
    public async Task HasPermission_Finance_GrantsPayouts_DeniesProfileEdit()
    {
        var membership = await ResolvedMembership(TenantRole.Finance, TenantType.Venue);

        Assert.True(membership.HasPermission(Permissions.PayoutsManage));
        Assert.True(membership.HasPermission(Permissions.SettlementTrigger));
        Assert.False(membership.HasPermission(Permissions.ProfileEdit));
        Assert.False(membership.HasPermission(Permissions.OpportunitiesManage, TenantType.Venue));
    }

    [Fact]
    public async Task HasPermission_Manager_RunsBusiness_ButNotPayouts()
    {
        var membership = await ResolvedMembership(TenantRole.Manager, TenantType.Venue);

        Assert.True(membership.HasPermission(Permissions.OpportunitiesManage, TenantType.Venue));
        Assert.True(membership.HasPermission(Permissions.ConcertsManage, TenantType.Venue));
        Assert.False(membership.HasPermission(Permissions.PayoutsManage));
    }
}
