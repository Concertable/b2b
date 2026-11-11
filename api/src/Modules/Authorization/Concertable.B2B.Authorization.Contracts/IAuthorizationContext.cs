using Reunion;

namespace Concertable.B2B.Authorization.Contracts;

public interface IAuthorizationContext
{
    Option<MembershipSnapshot> Membership { get; }
}
