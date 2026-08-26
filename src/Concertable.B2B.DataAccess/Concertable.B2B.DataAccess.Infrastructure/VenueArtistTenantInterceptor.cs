using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.B2B.DataAccess.Infrastructure;

public sealed class VenueArtistTenantInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Guard(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Guard(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Guard(DbContext? context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var entry in context.ChangeTracker.Entries<IVenueArtistTenantScoped>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.VenueTenantId == Guid.Empty || entry.Entity.ArtistTenantId == Guid.Empty)
                    throw new InvalidOperationException(
                        $"{entry.Entity.GetType().Name} was inserted without both tenant ids stamped.");
            }
            else if (entry.State == EntityState.Modified
                && (entry.Property(nameof(IVenueArtistTenantScoped.VenueTenantId)).IsModified
                    || entry.Property(nameof(IVenueArtistTenantScoped.ArtistTenantId)).IsModified))
            {
                throw new InvalidOperationException(
                    $"{entry.Entity.GetType().Name} tried to change its venue/artist tenant pair after creation.");
            }
        }
    }
}
