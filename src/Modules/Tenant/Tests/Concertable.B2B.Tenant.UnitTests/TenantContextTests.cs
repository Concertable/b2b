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
    private readonly DefaultHttpContext httpContext = new();
    private static readonly IPermissionCatalog Catalog = BuildCatalog();

    private static IPermissionCatalog BuildCatalog()
    {
        var shared = new SharedPermissions();
        return new PermissionCatalog(new VenuePermissions(shared), new ArtistPermissions(shared));
    }

    private TenantContext CreateContext() =>
        new(currentUser.Object, httpContextAccessor.Object, repository.Object, Catalog);

    private void WithHttpRequest() =>
        httpContextAccessor.SetupGet(h => h.HttpContext).Returns(httpContext);

    private void WithoutHttpRequest() =>
        httpContextAccessor.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

    private void WithTenantHeader(string value) =>
        httpContext.Request.Headers[TenantHeaders.TenantId] = value;

    private static UserMembership Membership(
        Guid tenantId, TenantRole role = TenantRole.Owner, TenantType type = TenantType.Venue) =>
        new(tenantId, "Acme Ltd", type, role);

    /// <summary>Resolve a context backed by a single membership of the given role + persona (no header → default).</summary>
    private async Task<IMembershipContext> ResolvedMembership(TenantRole role, TenantType type)
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetMembershipsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(Guid.NewGuid(), role, type)]);

        var context = CreateContext();
        await context.ResolveAsync();
        return context;
    }

    [Fact]
    public async Task ResolveAsync_SingleMembershipNoHeader_DefaultsToIt()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetMembershipsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(tenantId, TenantRole.Owner, TenantType.Venue)]);

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.Equal(tenantId, ctx.TenantId);
        Assert.True(ctx.HasTenant);
        Assert.False(ctx.IsHost);
        Assert.Equal(TenantRole.Owner, ((IMembershipContext)context).Role);
    }

    [Fact]
    public async Task ResolveAsync_MultipleMembershipsNoHeader_FailsClosed()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetMembershipsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(Guid.NewGuid()), Membership(Guid.NewGuid())]);

        var context = CreateContext();
        await context.ResolveAsync();

        // No header + several memberships: the caller must name one, so the request fails closed (sees nothing).
        ITenantContext ctx = context;
        Assert.Null(ctx.TenantId);
        Assert.False(ctx.HasTenant);
        Assert.Null(((IMembershipContext)context).Role);
    }

    [Fact]
    public async Task ResolveAsync_ValidHeader_ResolvesThatTenant_WithoutListingMemberships()
    {
        var userId = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();
        WithHttpRequest();
        WithTenantHeader(headerTenant.ToString());
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetMembershipAsync(userId, headerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Membership(headerTenant, TenantRole.Manager, TenantType.Artist));

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.Equal(headerTenant, ctx.TenantId);
        Assert.Equal(TenantRole.Manager, ((IMembershipContext)context).Role);
        Assert.True(((IMembershipContext)context).HasPermission(ArtistPermissions.ApplicationsSubmit, TenantType.Artist));
        repository.Verify(
            r => r.GetMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_HeaderForUnownedTenant_FailsClosed()
    {
        var userId = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();
        WithHttpRequest();
        WithTenantHeader(headerTenant.ToString());
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetMembershipAsync(userId, headerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserMembership?)null);

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.Null(ctx.TenantId);
        Assert.False(ctx.HasTenant);
    }

    [Fact]
    public async Task ResolveAsync_MalformedHeader_TreatedAsAbsent_DefaultsToSoleMembership()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        WithHttpRequest();
        WithTenantHeader("not-a-guid");
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetMembershipsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(tenantId)]);

        var context = CreateContext();
        await context.ResolveAsync();

        Assert.Equal(tenantId, ((ITenantContext)context).TenantId);
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
            r => r.GetMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
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
            r => r.GetMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithoutMembership_FailsClosedAndRoleIsNull()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetMembershipsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.False(ctx.IsHost);
        Assert.Null(ctx.TenantId);
        Assert.False(ctx.HasTenant);

        var membership = (IMembershipContext)context;
        Assert.Null(membership.Role);
        Assert.False(membership.HasPermission(SharedPermissions.OperationsView));
    }

    [Fact]
    public async Task HasPermission_OwnerOfVenue_GrantsVenueScopedPermission()
    {
        var membership = await ResolvedMembership(TenantRole.Owner, TenantType.Venue);

        Assert.True(membership.HasPermission(SharedPermissions.ProfileEdit, TenantType.Venue));
        Assert.True(membership.HasPermission(VenuePermissions.OpportunitiesManage, TenantType.Venue));
    }

    [Fact]
    public async Task HasPermission_WrongPersona_Denies_RightPersona_Grants()
    {
        var membership = await ResolvedMembership(TenantRole.Owner, TenantType.Artist);

        // The shared permission is reachable only on the matching surface; the venue-exclusive permission is
        // unreachable for an artist tenant by construction (its catalog has no applications.decide).
        Assert.False(membership.HasPermission(SharedPermissions.ProfileEdit, TenantType.Venue));
        Assert.True(membership.HasPermission(SharedPermissions.ProfileEdit, TenantType.Artist));
        Assert.True(membership.HasPermission(ArtistPermissions.ApplicationsSubmit, TenantType.Artist));
        Assert.False(membership.HasPermission(VenuePermissions.ApplicationsDecide, TenantType.Venue));
    }

    [Fact]
    public async Task HasPermission_Finance_GrantsPayouts_DeniesProfileEdit()
    {
        var membership = await ResolvedMembership(TenantRole.Finance, TenantType.Venue);

        Assert.True(membership.HasPermission(SharedPermissions.PayoutsManage));
        Assert.True(membership.HasPermission(SharedPermissions.SettlementTrigger));
        Assert.False(membership.HasPermission(SharedPermissions.ProfileEdit));
        Assert.False(membership.HasPermission(VenuePermissions.OpportunitiesManage, TenantType.Venue));
    }

    [Fact]
    public async Task HasPermission_Manager_RunsBusiness_ButNotPayouts()
    {
        var membership = await ResolvedMembership(TenantRole.Manager, TenantType.Venue);

        Assert.True(membership.HasPermission(VenuePermissions.OpportunitiesManage, TenantType.Venue));
        Assert.True(membership.HasPermission(VenuePermissions.ConcertsManage, TenantType.Venue));
        Assert.False(membership.HasPermission(SharedPermissions.PayoutsManage));
    }
}
