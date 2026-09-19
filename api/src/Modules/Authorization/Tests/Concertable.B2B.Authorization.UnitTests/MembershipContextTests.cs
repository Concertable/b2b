using Concertable.B2B.Authorization.Infrastructure.Authorization;
using Concertable.B2B.Authorization.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class MembershipContextTests
{
    public static TheoryData<TenantRole, TenantPermission, bool> RolePermissions => new()
    {
        { TenantRole.Finance, TenantPermission.PayoutsManage, true },
        { TenantRole.Finance, TenantPermission.ProfileEdit, false },
        { TenantRole.Manager, TenantPermission.OpportunitiesManage, true },
        { TenantRole.Manager, TenantPermission.PayoutsManage, false },
    };

    private readonly Mock<ICurrentUser> currentUser;
    private readonly Mock<IHttpContextAccessor> httpContextAccessor;
    private readonly Mock<IMembershipReadRepository> memberships;
    private readonly DefaultHttpContext httpContext;
    private readonly MembershipContext context;

    public MembershipContextTests()
    {
        this.currentUser = new Mock<ICurrentUser>();
        this.httpContextAccessor = new Mock<IHttpContextAccessor>();
        this.memberships = new Mock<IMembershipReadRepository>();
        this.httpContext = new DefaultHttpContext();
        this.context = new MembershipContext(
            this.currentUser.Object,
            this.httpContextAccessor.Object,
            this.memberships.Object,
            new PermissionCatalog(),
            new MembershipContextAccessor(this.httpContextAccessor.Object));
    }

    private void WithHttpRequest() =>
        this.httpContextAccessor.SetupGet(h => h.HttpContext).Returns(this.httpContext);

    private void WithoutHttpRequest() =>
        this.httpContextAccessor.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

    private static MembershipSnapshot Membership(Guid tenantId, TenantRole role = TenantRole.Owner) =>
        new(Guid.NewGuid(), tenantId, Guid.NewGuid(), role, PermissionVersion: 3);

    #region ResolveAsync

    [Fact]
    public async Task ResolveAsync_SingleMembershipNoHeader_DefaultsToIt()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        WithHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.memberships.Setup(f => f.GetSnapshotsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(tenantId)]);

        await this.context.ResolveAsync();

        Assert.Equal(tenantId, ((ITenantContext)this.context).TenantId);
        Assert.Equal(TenantRole.Owner, ((IMembershipContext)this.context).Membership?.Role);
        Assert.Equal(3, ((IMembershipContext)this.context).Membership?.PermissionVersion);
    }

    [Fact]
    public async Task ResolveAsync_MultipleMembershipsNoHeader_FailsClosed()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.memberships.Setup(f => f.GetSnapshotsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(Guid.NewGuid()), Membership(Guid.NewGuid())]);

        await this.context.ResolveAsync();

        Assert.Null(((ITenantContext)this.context).TenantId);
        Assert.Null(((IMembershipContext)this.context).Membership?.Role);
    }

    [Fact]
    public async Task ResolveAsync_ValidHeader_ResolvesThatTenantAlone()
    {
        var userId = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();
        WithHttpRequest();
        this.httpContext.Request.Headers[TenantHeaders.TenantId] = headerTenant.ToString();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.memberships.Setup(f => f.GetSnapshotByUserIdAndTenantIdAsync(userId, headerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Membership(headerTenant, TenantRole.Manager));

        await this.context.ResolveAsync();

        Assert.Equal(headerTenant, ((ITenantContext)this.context).TenantId);
        Assert.Equal(TenantRole.Manager, ((IMembershipContext)this.context).Membership?.Role);
    }

    [Fact]
    public async Task ResolveAsync_HeaderForUnownedTenant_FailsClosed()
    {
        var userId = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();
        WithHttpRequest();
        this.httpContext.Request.Headers[TenantHeaders.TenantId] = headerTenant.ToString();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.memberships.Setup(f => f.GetSnapshotByUserIdAndTenantIdAsync(userId, headerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipSnapshot?)null);

        await this.context.ResolveAsync();

        Assert.Null(((ITenantContext)this.context).TenantId);
    }

    [Fact]
    public async Task ResolveAsync_MalformedHeader_Throws()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        this.httpContext.Request.Headers[TenantHeaders.TenantId] = "not-a-guid";
        this.currentUser.SetupGet(u => u.Id).Returns(userId);

        await Assert.ThrowsAsync<MalformedTenantHeaderException>(() => this.context.ResolveAsync());
    }

    [Fact]
    public async Task ResolveAsync_AnonymousRequest_FailsClosed()
    {
        WithHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns((Guid?)null);

        await this.context.ResolveAsync();

        Assert.Null(((ITenantContext)this.context).TenantId);
        Assert.False(((ITenantContext)this.context).IsHost);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithoutMembership_HasNoRoleAndNoPermission()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.memberships.Setup(f => f.GetSnapshotsByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await this.context.ResolveAsync();

        Assert.Null(((IMembershipContext)this.context).Membership?.Role);
        Assert.False(((IMembershipContext)this.context).HasPermission(TenantPermission.OperationsView));
    }

    [Fact]
    public async Task ResolveAsync_NoRequest_ResolvesNothing()
    {
        WithoutHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns(Guid.NewGuid());

        await this.context.ResolveAsync();

        Assert.Null(((ITenantContext)this.context).TenantId);
    }

    #endregion

    #region IsHost

    [Fact]
    public void IsHost_IsNeverTrue()
    {
        WithoutHttpRequest();

        Assert.False(((ITenantContext)this.context).IsHost);
    }

    #endregion

    #region HasPermission

    [Theory]
    [MemberData(nameof(RolePermissions))]
    public async Task HasPermission_ResolvedMembership_ReadsTheRoleBundleAlone(
        TenantRole role,
        TenantPermission permission,
        bool expected)
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.memberships.Setup(f => f.GetSnapshotsByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(Guid.NewGuid(), role)]);

        await this.context.ResolveAsync();

        Assert.Equal(expected, ((IMembershipContext)this.context).HasPermission(permission));
    }

    #endregion
}
