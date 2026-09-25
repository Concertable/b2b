using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess.Infrastructure;

public static class TenantFilters
{
    public const string Key = "Tenant";

    public static void ApplySingleOwner<TEntity>(this ModelBuilder modelBuilder, IHasTenantContext context)
        where TEntity : class, ITenantScoped =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(Key, e =>
            e.TenantId == context.TenantContext.TenantId);
}
