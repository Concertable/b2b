using System.Net;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]
public sealed class TenantScopingTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public TenantScopingTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Apply_StampsBothPartyTenantsOnTheApplication()
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var opportunityResponse = await venueClient.PostAsync(
            "/api/opportunity",
            BuildOpportunityRequest());
        await opportunityResponse.ShouldBe(HttpStatusCode.Created);
        var opportunity = await opportunityResponse.Content.ReadAsync<OpportunityBoundaryResponse>();
        Assert.NotNull(opportunity);
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var applyResponse = await artistClient.PostAsync(
            $"/api/application/{opportunity.Id}",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var application = await fixture.Applications
            .SingleAsync(value => value.OpportunityId == opportunity.Id);
        Assert.Equal(TenantOf(fixture.SeedState.VenueManager1.Id), application.VenueTenantId);
        Assert.Equal(TenantOf(fixture.SeedState.ArtistManager1.Id), application.ArtistTenantId);
    }

    [Fact]
    public async Task Application_IsVisibleToBothPartiesAndInvisibleToThirdPartyTenants()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var venueParty = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var artistParty = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var thirdParty = fixture.CreateClient(fixture.SeedState.VenueManager2);

        await (await venueParty.GetAsync($"/api/application/{applicationId}"))
            .ShouldBe(HttpStatusCode.OK);
        await (await artistParty.GetAsync($"/api/application/{applicationId}"))
            .ShouldBe(HttpStatusCode.OK);
        await (await thirdParty.GetAsync($"/api/application/{applicationId}"))
            .ShouldBe(HttpStatusCode.NotFound);
    }

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(value => value.CreatedByUserId == userId).Id;

    private OpportunityBoundaryRequest BuildOpportunityRequest() =>
        new(
            fixture.SeedNow.AddMonths(1),
            fixture.SeedNow.AddMonths(1).AddHours(3),
            [Genre.Rock],
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Cash, Fee = 500m });

    private sealed record OpportunityBoundaryRequest(
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<Genre> Genres,
        DealDto Deal);

    private sealed record OpportunityBoundaryResponse(int Id);
}
