using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class AuthorizationContextExtensionsTests
{
    [Fact]
    public void TryGetMembership_MembershipInTheTenant_ReturnsIt()
    {
        var tenantId = Guid.NewGuid();
        var actor = new MembershipSnapshot(Guid.NewGuid(), tenantId, Guid.NewGuid(), 1, 1, default);
        var context = Mock.Of<IAuthorizationContext>(value => value.Membership == actor);

        Assert.True(context.TryGetMembership(tenantId, out var found));
        Assert.Same(actor, found);
        Assert.False(context.TryGetMembership(Guid.NewGuid(), out _));
        Assert.False(Mock.Of<IAuthorizationContext>().TryGetMembership(tenantId, out _));
    }
}
