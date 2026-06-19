using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Domain;
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

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithTenant_ResolvesThatTenant()
    {
        var userId = Guid.NewGuid();
        var tenant = TenantEntity.Create("Acme Ltd", userId, DateTime.UtcNow);
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetByCreatedByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.Equal(tenant.Id, ctx.TenantId);
        Assert.True(ctx.HasTenant);
        Assert.False(ctx.IsHost);
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
            r => r.GetByCreatedByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
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
            r => r.GetByCreatedByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithoutTenant_NotHostAndFailsClosed()
    {
        var userId = Guid.NewGuid();
        WithHttpRequest();
        currentUser.SetupGet(u => u.Id).Returns(userId);
        repository.Setup(r => r.GetByCreatedByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantEntity?)null);

        var context = CreateContext();
        await context.ResolveAsync();

        ITenantContext ctx = context;
        Assert.False(ctx.IsHost);
        Assert.Null(ctx.TenantId);
        Assert.False(ctx.HasTenant);
    }
}
