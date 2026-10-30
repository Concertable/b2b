using System.Linq.Expressions;
using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.DataAccess.Application;

public sealed class MembershipAuthority
{
    public Guid MembershipId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public long PermissionVersion { get; private set; }
    public long RolePolicyVersion { get; private set; }

    public static Expression<Func<MembershipAuthority, bool>> Matches(MembershipSnapshot actor) =>
        member => member.MembershipId == actor.MembershipId
            && member.TenantId == actor.TenantId
            && member.UserId == actor.UserId
            && member.PermissionVersion == actor.PermissionVersion
            && member.RolePolicyVersion == actor.RolePolicyVersion;
}
