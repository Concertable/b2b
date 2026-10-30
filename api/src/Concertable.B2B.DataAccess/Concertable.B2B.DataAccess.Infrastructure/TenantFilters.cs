using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The named "Tenant" query filter: its key (for <c>IgnoreQueryFilters([TenantFilters.Key])</c>)
/// and its per-entity registrations, called from a context's <c>OnModelCreating</c>. Whether an
/// entity is filtered is a per-entity product decision — a marked entity may stay public
/// (e.g. Concert, whose details page is marketplace browse).
/// </summary>
public static class TenantFilters
{
    public const string Key = "Tenant";

    public static void ApplySingleOwner<TEntity>(this ModelBuilder modelBuilder, IHasTenantContext context)
        where TEntity : class, ITenantScoped =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(Key, e =>
            e.TenantId == context.TenantContext.TenantId);
}
