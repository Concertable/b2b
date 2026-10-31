using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class AuthorizationContextExtensionsTests
{
    [Fact]
    public void FindActor_MembershipInTheTenant_ReturnsIt()
    {
        var tenantId = Guid.NewGuid();
        var actor = new MembershipSnapshot(Guid.NewGuid(), tenantId, Guid.NewGuid(), 1, 1, default);
        var context = Mock.Of<IAuthorizationContext>(value => value.Membership == actor);

        Assert.Same(actor, context.FindActor(tenantId));
        Assert.Null(context.FindActor(Guid.NewGuid()));
        Assert.Null(Mock.Of<IAuthorizationContext>().FindActor(tenantId));
    }
}
