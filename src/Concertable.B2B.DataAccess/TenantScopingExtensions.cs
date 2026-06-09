using System.Linq.Expressions;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.DataAccess;

public static class TenantScopingExtensions
{
    public const string TenantFilter = "Tenant";

    /// <summary>
    /// Applies the single-owner tenant filter to every <see cref="ITenantScoped"/> entity in the model.
    /// Two-party (operator/artist) entities get their OR-filter explicitly in their own config.
    /// </summary>
    public static ModelBuilder ApplyTenantScoping(this ModelBuilder modelBuilder, ITenantContext tenantContext)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
                continue;

            var entity = Expression.Parameter(entityType.ClrType, "e");
            var context = Expression.Constant(tenantContext, typeof(ITenantContext));

            var isHost = Expression.Property(context, nameof(ITenantContext.IsHost));
            var entityTenantId = Expression.Property(entity, nameof(ITenantScoped.TenantId));   // Guid
            var currentTenantId = Expression.Property(context, nameof(ITenantContext.TenantId)); // Guid?

            var matchesTenant = Expression.Equal(
                Expression.Convert(entityTenantId, typeof(Guid?)),
                currentTenantId);

            var filter = Expression.Lambda(Expression.OrElse(isHost, matchesTenant), entity);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(TenantFilter, filter);
        }

        return modelBuilder;
    }
}
