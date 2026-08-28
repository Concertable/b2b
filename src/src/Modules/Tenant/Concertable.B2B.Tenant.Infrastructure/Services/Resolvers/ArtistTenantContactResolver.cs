using IArtistModule = Concertable.B2B.Artist.Contracts.IArtistModule;

namespace Concertable.B2B.Tenant.Infrastructure.Services.Resolvers;

internal sealed class ArtistTenantContactResolver : ITenantContactResolver
{
    private readonly IArtistModule artistModule;

    public ArtistTenantContactResolver(IArtistModule artistModule)
    {
        this.artistModule = artistModule;
    }

    public async Task<Option<TenantContact>> ResolveAsync(
        TenantType type,
        Guid tenantId,
        CancellationToken ct = default) =>
        (await artistModule.GetContactByTenantIdAsync(tenantId, ct))
            .Map(contact => new TenantContact(contact.Name, contact.Email));
}
