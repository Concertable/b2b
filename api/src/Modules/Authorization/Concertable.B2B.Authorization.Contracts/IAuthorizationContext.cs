namespace Concertable.B2B.Authorization.Contracts;

public interface IAuthorizationContext
{
    MembershipSnapshot? Membership { get; }
}
