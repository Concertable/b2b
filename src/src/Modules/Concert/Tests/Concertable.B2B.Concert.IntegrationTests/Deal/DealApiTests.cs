using System.Net;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Deal;

[Collection("Integration")]
public sealed class DealApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public DealApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task GetById_ExistingDeal_ReturnsDeal()
    {
        var client = fixture.CreateClient();
        var expected = fixture.SeedState.FreshVenueHireOpportunity;

        var response = await client.GetAsync($"/api/Deal/{expected.DealId}");

        await response.ShouldBe(HttpStatusCode.OK);
        var deal = await response.Content.ReadAsync<DealDto>();
        Assert.NotNull(deal);
        Assert.Equal(expected.DealId, deal.Id);
        Assert.Equal(DealType.VenueHire, deal.DealType);
    }

    [Fact]
    public async Task GetById_MissingDeal_ReturnsNotFoundProblem()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Deal/2147483647");

        await response.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Extensions.TryGetValue("code", out var code));
        Assert.Equal("deal.get.not_found", code?.ToString());
        Assert.Equal("Deal 2147483647 was not found.", problem.Detail);
    }
}
