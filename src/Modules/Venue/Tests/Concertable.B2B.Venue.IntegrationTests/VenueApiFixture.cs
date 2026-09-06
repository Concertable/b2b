using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Venue.Domain.Entities;
using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Venue.IntegrationTests;

public sealed class VenueApiFixture : ApiFixture
{
    private IVenueReadDbContext dbContext = null!;

    public IQueryable<VenueEntity> Venues => dbContext.Venues;

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<IVenueReadDbContext>();
    }
}
