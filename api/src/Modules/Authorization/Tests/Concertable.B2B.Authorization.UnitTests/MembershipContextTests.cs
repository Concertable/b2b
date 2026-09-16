using Concertable.B2B.Authorization.Infrastructure.Authorization;
using Concertable.B2B.Authorization.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class MembershipContextTests
{
    private readonly Mock<ICurrentUser> currentUser;
    private readonly Mock<IHttpContextAccessor> httpContextAccessor;
    private readonly Mock<IMembershipFacts> facts;
    private readonly DefaultHttpContext httpContext;
    private readonly ExecutionScope executionScope;
    private readonly MembershipContext context;

    public MembershipContextTests()
    {
        this.currentUser = new Mock<ICurrentUser>();
        this.httpContextAccessor = new Mock<IHttpContextAccessor>();
        this.facts = new Mock<IMembershipFacts>();
        this.httpContext = new DefaultHttpContext();
        this.executionScope = new ExecutionScope();
        this.context = new MembershipContext(
            this.currentUser.Object,
            this.httpContextAccessor.Object,
            this.facts.Object,
            new PermissionCatalog(),
            this.executionScope,
            new MembershipContextAccessor(this.httpContextAccessor.Object));
    }

    private void WithHttpRequest() =>
        this.httpContextAccessor.SetupGet(h => h.HttpContext).Returns(this.httpContext);

    private void WithoutHttpRequest() =>
        this.httpContextAccessor.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

    private static MembershipFact Membership(Guid tenantId, TenantRole role = TenantRole.Owner) =>
        new(tenantId, Guid.NewGuid(), role, AuthorizationVersion: 3);

    #region ResolveAsync

    [Fact]
    public async Task ResolveAsync_SingleMembershipNoHeader_DefaultsToIt()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        WithHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.facts.Setup(f => f.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(tenantId)]);

        await this.context.ResolveAsync();

        Assert.Equal(tenantId, ((ITenantContext)this.context).TenantId);
        Assert.Equal(TenantRole.Owner, ((IMembershipContext)this.context).Role);
        Assert.Equal(3, ((IMembershipContext)this.context).AuthorizationVersion);
    }

    [Fact]
    public async Task ResolveAsync_MultipleMembershipsNoHeader_FailsClosed()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.facts.Setup(f => f.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(Guid.NewGuid()), Membership(Guid.NewGuid())]);

        await this.context.ResolveAsync();

        Assert.Null(((ITenantContext)this.context).TenantId);
        Assert.Null(((IMembershipContext)this.context).Role);
    }

    [Fact]
    public async Task ResolveAsync_ValidHeader_ResolvesThatTenantAlone()
    {
        var userId = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();
        WithHttpRequest();
        this.httpContext.Request.Headers[TenantHeaders.TenantId] = headerTenant.ToString();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.facts.Setup(f => f.GetAsync(userId, headerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Membership(headerTenant, TenantRole.Manager));

        await this.context.ResolveAsync();

        Assert.Equal(headerTenant, ((ITenantContext)this.context).TenantId);
        Assert.Equal(TenantRole.Manager, ((IMembershipContext)this.context).Role);
    }

    [Fact]
    public async Task ResolveAsync_HeaderForUnownedTenant_FailsClosed()
    {
        var userId = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();
        WithHttpRequest();
        this.httpContext.Request.Headers[TenantHeaders.TenantId] = headerTenant.ToString();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.facts.Setup(f => f.GetAsync(userId, headerTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipFact?)null);

        await this.context.ResolveAsync();

        Assert.Null(((ITenantContext)this.context).TenantId);
    }

    [Fact]
    public async Task ResolveAsync_MalformedHeader_TreatedAsAbsentAndDefaultsToSoleMembership()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        WithHttpRequest();
        this.httpContext.Request.Headers[TenantHeaders.TenantId] = "not-a-guid";
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.facts.Setup(f => f.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(tenantId)]);

        await this.context.ResolveAsync();

        Assert.Equal(tenantId, ((ITenantContext)this.context).TenantId);
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
        this.facts.Setup(f => f.GetAllAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await this.context.ResolveAsync();

        Assert.Null(((IMembershipContext)this.context).Role);
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
    public void IsHost_RequestFreeCallerThatEstablishedNothing_IsFalse()
    {
        WithoutHttpRequest();

        Assert.False(((ITenantContext)this.context).IsHost);
    }

    [Fact]
    public void IsHost_EstablishedExecutionScope_IsTrueOnlyForItsLifetime()
    {
        WithoutHttpRequest();

        using (this.executionScope.Enter(ExecutionPurpose.System))
            Assert.True(((ITenantContext)this.context).IsHost);

        Assert.False(((ITenantContext)this.context).IsHost);
    }

    [Fact]
    public void IsHost_InteractiveStanceInsideATrustedOne_IsFalse()
    {
        WithoutHttpRequest();

        using var system = this.executionScope.Enter(ExecutionPurpose.System);
        using var interactive = this.executionScope.EnterInteractive();

        Assert.False(((ITenantContext)this.context).IsHost);
    }

    #endregion

    #region HasPermission

    [Theory]
    [InlineData(TenantRole.Finance, TenantPermission.PayoutsManage, true)]
    [InlineData(TenantRole.Finance, TenantPermission.ProfileEdit, false)]
    [InlineData(TenantRole.Manager, TenantPermission.OpportunitiesManage, true)]
    [InlineData(TenantRole.Manager, TenantPermission.PayoutsManage, false)]
    [InlineData(TenantRole.RestrictedParticipant, TenantPermission.OperationsView, true)]
    [InlineData(TenantRole.RestrictedParticipant, TenantPermission.MessagesRead, false)]
    public async Task HasPermission_ResolvedMembership_ReadsTheRoleBundleAlone(
        TenantRole role,
        string permission,
        bool expected)
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        this.currentUser.SetupGet(u => u.Id).Returns(userId);
        this.facts.Setup(f => f.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Membership(Guid.NewGuid(), role)]);

        await this.context.ResolveAsync();

        Assert.Equal(expected, ((IMembershipContext)this.context).HasPermission(permission));
    }

    #endregion
}
