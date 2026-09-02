using Concertable.Kernel.Specifications;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// Scalar reads off a two-party row, for any <see cref="IVenueArtistTenantScoped"/> entity. Each projects
/// without loading the row, so a caller takes the party ids it needs through the projecting
/// <c>GetByIdAsync</c> overload rather than a per-entity finder.
/// </summary>
public sealed class VenueArtistTenantSpecification<TEntity> : SpecificationBuilder<TEntity>
    where TEntity : class, IVenueArtistTenantScoped
{
    public static ISpecification<TEntity, TenantPair?> CreatePair() =>
        new VenueArtistTenantSpecification<TEntity>()
            .Select(entity => new TenantPair(entity.VenueTenantId, entity.ArtistTenantId));

    public static ISpecification<TEntity, Guid?> CreateVenueTenantId() =>
        new VenueArtistTenantSpecification<TEntity>()
            .Select(entity => (Guid?)entity.VenueTenantId);

    public static ISpecification<TEntity, Guid?> CreateArtistTenantId() =>
        new VenueArtistTenantSpecification<TEntity>()
            .Select(entity => (Guid?)entity.ArtistTenantId);
}
