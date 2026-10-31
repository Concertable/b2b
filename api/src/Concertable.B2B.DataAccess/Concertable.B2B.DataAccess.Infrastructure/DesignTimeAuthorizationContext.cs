using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class DesignTimeAuthorizationContext : IAuthorizationContext
{
    public static readonly DesignTimeAuthorizationContext Instance = new();

    public MembershipSnapshot? Membership => null;
}
