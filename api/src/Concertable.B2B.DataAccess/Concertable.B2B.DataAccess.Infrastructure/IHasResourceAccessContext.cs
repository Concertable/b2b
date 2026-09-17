using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// A DbContext whose rows are reached through resource access grants. Query filters read the acting membership
/// THROUGH the context instance, never through a captured scoped service: EF caches the model once and
/// re-binds context references per query, so a captured request-scoped value would freeze the first request's
/// caller forever.
/// <para>
/// The nullable scalars exist because a filter cannot dereference <see cref="IResourceAccessContext.Membership"/>:
/// the whole member access is evaluated before the query runs, so an unresolved caller would throw instead of
/// reading nothing. The authority relation is a <see cref="DbSet{T}"/> rather than an
/// <see cref="IQueryable{T}"/> because only a DbSet is a query root — an IQueryable member is parameterised
/// and its <c>Any</c> then cannot translate. Its keylessness, not its type, is what makes it unwritable.
/// </para>
/// </summary>
public interface IHasResourceAccessContext
{
    IResourceAccessContext ResourceAccess { get; }

    DbSet<MembershipAuthority> MembershipAuthority { get; }

    Guid? ActiveMembershipId { get; }
    Guid? ActiveTenantId { get; }
    Guid? ActiveUserId { get; }
    long? ActivePermissionVersion { get; }

    ResourceAudience AudienceFor(string permission);
}
