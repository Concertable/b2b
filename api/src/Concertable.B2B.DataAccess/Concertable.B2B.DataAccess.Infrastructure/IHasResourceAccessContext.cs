using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public interface IHasResourceAccessContext
{
    IResourceAccessContext ResourceAccess { get; }

    // Must stay a DbSet: an IQueryable member is parameterised whole and its Any then cannot translate.
    DbSet<MembershipAuthority> MembershipAuthority { get; }

    // A filter cannot dereference ResourceAccess.Membership — the member access is evaluated before the
    // query runs, so an unresolved caller would throw rather than read nothing.
    Guid? ActiveMembershipId { get; }
    Guid? ActiveTenantId { get; }
    Guid? ActiveUserId { get; }
    long? ActivePermissionVersion { get; }

    ResourceAudience AudienceFor(TenantPermission permission);
}
