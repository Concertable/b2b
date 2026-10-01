using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class MembershipContextTests
{
    private readonly Mock<ICurrentUser> currentUser;
    private readonly Mock<IHttpContextAccessor> httpContextAccessor;
    private readonly Mock<IMembershipReadRepository> memberships;
    private readonly DefaultHttpContext httpContext;
    private readonly MembershipContext context;

    public MembershipContextTests()
    {
        currentUser = new Mock<ICurrentUser>();
        httpContextAccessor = new Mock<IHttpContextAccessor>();
        memberships = new Mock<IMembershipReadRepository>();
        httpContext = new DefaultHttpContext();
        context = new MembershipContext(
            currentUser.Object,
            httpContextAccessor.Object,
            memberships.Object,
            new MembershipContextAccessor(httpContextAccessor.Object));
    }

    private void WithHttpRequest() =>
        httpContextAccessor.SetupGet(value => value.HttpContext).Returns(httpContext);

    private void WithoutHttpRequest() =>
        httpContextAccessor.SetupGet(value => value.HttpContext).Returns((HttpContext?)null);

    private static MembershipSnapshot Membership(
        Guid tenantId,
        IReadOnlyDictionary<TenantPermission, ResourceAudience>? permissions = null) =>
        new(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            3,
            4,
            (permissions ?? ImmutableDictionary<TenantPermission, ResourceAudience>.Empty).ToImmutableDictionary());

    [Fact]
    public async Task ResolveAsync_SingleMembershipNoHeader_DefaultsToIt()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(value => value.Id).Returns(userId);
        memberships.Setup(value => value.GetSnapshotsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(tenantId)]);

        await context.ResolveAsync();

        Assert.Equal(tenantId, ((ITenantContext)context).TenantId);
        Assert.Equal(3, ((IMembershipContext)context).Membership?.PermissionVersion);
        Assert.Equal(4, ((IMembershipContext)context).Membership?.RolePolicyVersion);
    }

    [Fact]
    public async Task ResolveAsync_MultipleMembershipsNoHeader_FailsClosed()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(value => value.Id).Returns(userId);
        memberships.Setup(value => value.GetSnapshotsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(Guid.NewGuid()), Membership(Guid.NewGuid())]);

        await context.ResolveAsync();

        Assert.Null(((ITenantContext)context).TenantId);
        Assert.Null(((IMembershipContext)context).Membership);
    }

    [Fact]
    public async Task ResolveAsync_ValidHeader_ResolvesThatTenantAlone()
    {
        var userId = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();
        WithHttpRequest();
        httpContext.Request.Headers[TenantHeaders.TenantId] = headerTenant.ToString();
        currentUser.SetupGet(value => value.Id).Returns(userId);
        memberships.Setup(value => value.GetSnapshotByUserIdAndTenantIdAsync(userId, headerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Membership(headerTenant));

        await context.ResolveAsync();

        Assert.Equal(headerTenant, ((ITenantContext)context).TenantId);
    }

    [Fact]
    public async Task ResolveAsync_HeaderForUnownedTenant_FailsClosed()
    {
        var userId = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();
        WithHttpRequest();
        httpContext.Request.Headers[TenantHeaders.TenantId] = headerTenant.ToString();
        currentUser.SetupGet(value => value.Id).Returns(userId);
        memberships.Setup(value => value.GetSnapshotByUserIdAndTenantIdAsync(userId, headerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipSnapshot?)null);

        await context.ResolveAsync();

        Assert.Null(((ITenantContext)context).TenantId);
    }

    [Fact]
    public async Task ResolveAsync_MalformedHeader_Throws()
    {
        WithHttpRequest();
        httpContext.Request.Headers[TenantHeaders.TenantId] = "not-a-guid";
        currentUser.SetupGet(value => value.Id).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<MalformedTenantHeaderException>(() => context.ResolveAsync());
    }

    [Fact]
    public async Task ResolveAsync_AnonymousRequest_FailsClosed()
    {
        WithHttpRequest();
        currentUser.SetupGet(value => value.Id).Returns((Guid?)null);

        await context.ResolveAsync();

        Assert.Null(((ITenantContext)context).TenantId);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithoutMembership_HasNoPermission()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(value => value.Id).Returns(userId);
        memberships.Setup(value => value.GetSnapshotsByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await context.ResolveAsync();

        Assert.Null(((IMembershipContext)context).Membership);
        Assert.False(((IMembershipContext)context).HasPermission(TenantPermission.OperationsView));
    }

    [Fact]
    public async Task ResolveAsync_NoRequest_ResolvesNothing()
    {
        WithoutHttpRequest();
        currentUser.SetupGet(value => value.Id).Returns(Guid.NewGuid());

        await context.ResolveAsync();

        Assert.Null(((ITenantContext)context).TenantId);
    }

    [Fact]
    public async Task HasPermission_ResolvedMembership_UsesEffectivePermissionSnapshot()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = TenantPermission.PayoutsManage;
        WithHttpRequest();
        currentUser.SetupGet(value => value.Id).Returns(userId);
        memberships.Setup(value => value.GetSnapshotsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(tenantId, new Dictionary<TenantPermission, ResourceAudience>
            {
                [permission] = ResourceAudience.TenantResources,
            })]);

        await context.ResolveAsync();

        Assert.True(((IMembershipContext)context).HasPermission(permission));
        Assert.Equal(ResourceAudience.TenantResources, ((IMembershipContext)context).AudienceFor(permission));
        Assert.False(((IMembershipContext)context).HasPermission(TenantPermission.ProfileEdit));
    }
}
