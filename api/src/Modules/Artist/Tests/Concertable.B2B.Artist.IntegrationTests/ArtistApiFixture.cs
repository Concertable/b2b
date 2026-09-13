using Concertable.B2B.Artist.Domain.Entities;
using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Artist.IntegrationTests;

public sealed class ArtistApiFixture : ApiFixture
{
    private IArtistReadDbContext dbContext = null!;

    public IQueryable<ArtistEntity> Artists => dbContext.Artists;

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<IArtistReadDbContext>();
    }
}
