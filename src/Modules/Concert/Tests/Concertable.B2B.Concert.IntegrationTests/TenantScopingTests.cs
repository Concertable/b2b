using System.Net;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests;

[Collection("Integration")]
public sealed class TenantScopingTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public TenantScopingTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Concert_DetailsStayPubliclyReadableAcrossTenants()
    {
        var postedConcert = fixture.SeedState.Concerts.First(value => value.DatePosted is not null);
        var thirdParty = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);

        var response = await thirdParty.GetAsync($"/api/concert/{postedConcert.Id}");

        await response.ShouldBe(HttpStatusCode.OK);
    }
}
