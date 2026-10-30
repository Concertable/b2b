using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.Infrastructure;

public static class CurrentMemberQueryExtensions
{
    extension<T>(IQueryable<T> source)
    {
        public IQueryable<T> WhereCurrentMember(IQueryable<MembershipAuthority> members, MembershipSnapshot actor)
        {
            var current = MembershipAuthority.Matches(actor);
            return source.Where(_ => members.Any(current));
        }
    }
}
