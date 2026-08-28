using IVenueModule = Concertable.B2B.Venue.Contracts.IVenueModule;

namespace Concertable.B2B.Tenant.Infrastructure.Services.Resolvers;

internal sealed class VenueTenantContactResolver : ITenantContactResolver
{
    private readonly IVenueModule venueModule;

    public VenueTenantContactResolver(IVenueModule venueModule)
    {
        this.venueModule = venueModule;
    }

    public async Task<Option<TenantContact>> ResolveAsync(
        TenantType type,
        Guid tenantId,
        CancellationToken ct = default) =>
        (await venueModule.GetContactByTenantIdAsync(tenantId, ct))
            .Map(contact => new TenantContact(contact.Name, contact.Email));
}
