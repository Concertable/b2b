using Concertable.B2B.Seed.Contracts;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.Extensions.Time.Testing;

namespace Concertable.B2B.Concert.UnitTests.Seed;

public sealed class ConcertSeedParityTests
{
    [Fact]
    public void CatalogConcerts_MatchConcertsCreatedByTheProducerSeed()
    {
        var catalog = new SeedCatalog(new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero)));
        var seed = new SeedState(catalog);

        Assert.Equal(seed.Concerts.Select(concert => concert.Id).Order(), catalog.Concerts.Select(concert => concert.ConcertId).Order());
        Assert.DoesNotContain(catalog.Concerts, concert => concert.ConcertId == seed.AwaitingPaymentBooking.Id);
    }
}
