using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// A DbContext whose rows are reached through resource access grants. Query filters read the acting tenant,
/// member and authority revision THROUGH the context instance, never through a captured scoped service: EF
/// caches the model once and re-binds context references per query, so a captured request-scoped value would
/// freeze the first request's caller forever.
/// </summary>
public interface IHasAccessContext
{
    IAccessContext AccessContext { get; }

    /// <summary>Tenant's authority relation, mapped read-only so an access predicate can re-check the
    /// membership revision inside the same statement that reads the resource.</summary>
    DbSet<MembershipAuthorityFact> MembershipAuthority { get; }
}
