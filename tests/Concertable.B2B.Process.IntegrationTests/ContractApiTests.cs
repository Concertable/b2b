using System.Net;
using System.Text;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;
using Xunit.Abstractions;

namespace Concertable.B2B.Process.IntegrationTests;

[Collection("Integration")]
public sealed class ContractApiTests : IAsyncLifetime
{
    private readonly ProcessApiFixture fixture;

    public ContractApiTests(ProcessApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region Get

    [Fact]
    public async Task Get_ReturnsImmutableFlatFeeSnapshot()
    {
        var opportunityId = await CreateOpportunityAsync(
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{applicationId}/checkout");

        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var contract = await GetContractAsync(applicationId);
        Assert.Equal(DealType.FlatFee, contract.DealType);
        Assert.Equal(PaymentMethod.Transfer, contract.PaymentMethod);
        Assert.Equal("The venue pays the artist a flat fee of £500.00.", contract.TermsText);
        AssertCommonSnapshot(contract);

        await UpdateDealAsync(
            opportunityId,
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Cash, Fee = 999m });

        var frozen = await GetContractAsync(applicationId);
        Assert.Equal(PaymentMethod.Transfer, frozen.PaymentMethod);
        Assert.Equal("The venue pays the artist a flat fee of £500.00.", frozen.TermsText);
    }

    [Fact]
    public async Task Get_ReturnsImmutableDoorSplitSnapshot()
    {
        var opportunityId = await CreateOpportunityAsync(
            new DoorSplitDealDto { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new
            {
                eSignature = new { signatoryName = "Test Signatory" },
                paymentMethodId = "pm_card_visa"
            });

        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var contract = await GetContractAsync(applicationId);
        Assert.Equal(DealType.DoorSplit, contract.DealType);
        Assert.Equal("The artist receives 70% of door revenue.", contract.TermsText);
        AssertCommonSnapshot(contract);

        await UpdateDealAsync(
            opportunityId,
            new DoorSplitDealDto { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 15m });

        var frozen = await GetContractAsync(applicationId);
        Assert.Equal("The artist receives 70% of door revenue.", frozen.TermsText);
    }

    [Fact]
    public async Task Get_ReturnsImmutableVersusSnapshot()
    {
        var opportunityId = await CreateOpportunityAsync(
            new VersusDealDto
            {
                PaymentMethod = PaymentMethod.Cash,
                Guarantee = 200m,
                ArtistDoorPercent = 60m
            });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new
            {
                eSignature = new { signatoryName = "Test Signatory" },
                paymentMethodId = "pm_card_visa"
            });

        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var contract = await GetContractAsync(applicationId);
        Assert.Equal(DealType.Versus, contract.DealType);
        Assert.Equal(
            "The artist receives a guarantee of £200.00 plus 60% of door revenue.",
            contract.TermsText);
        AssertCommonSnapshot(contract);

        await UpdateDealAsync(
            opportunityId,
            new VersusDealDto
            {
                PaymentMethod = PaymentMethod.Cash,
                Guarantee = 999m,
                ArtistDoorPercent = 10m
            });

        var frozen = await GetContractAsync(applicationId);
        Assert.Equal(
            "The artist receives a guarantee of £200.00 plus 60% of door revenue.",
            frozen.TermsText);
    }

    [Fact]
    public async Task Get_ReturnsImmutableVenueHireSnapshot()
    {
        var opportunityId = await CreateOpportunityAsync(
            new VenueHireDealDto { PaymentMethod = PaymentMethod.Cash, HireFee = 250m });
        var applicationId = await ApplyAsync(opportunityId, "pm_card_visa");
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var contract = await GetContractAsync(applicationId);
        Assert.Equal(DealType.VenueHire, contract.DealType);
        Assert.Equal("The artist pays the venue a hire fee of £250.00.", contract.TermsText);
        AssertCommonSnapshot(contract);

        await UpdateDealAsync(
            opportunityId,
            new VenueHireDealDto { PaymentMethod = PaymentMethod.Cash, HireFee = 999m });

        var frozen = await GetContractAsync(applicationId);
        Assert.Equal("The artist pays the venue a hire fee of £250.00.", frozen.TermsText);
    }

    [Fact]
    public async Task Get_ReturnsSignaturesForSeededAcceptedApplication()
    {
        var applicationId = fixture.SeedState.FlatFeeApp.Id;
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{applicationId}/checkout");

        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });

        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var contract = await GetContractAsync(applicationId);
        Assert.NotNull(contract.ArtistSignature);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, contract.VenueSignature.UserId);
    }

    [Fact]
    public async Task Get_IsReadableByParty_And404ForStranger()
    {
        var applicationId = await AcceptedFlatFeeAsync();
        var artist = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await artist.GetAsync($"/api/application/{applicationId}/contract");

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("The venue pays the artist a flat fee of", body);
        Assert.Contains("2026-07", body);
        var stranger = fixture.CreateClient(fixture.SeedState.VenueManager2);
        var strangerResponse = await stranger.GetAsync($"/api/application/{applicationId}/contract");
        await strangerResponse.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetPdf

    [Fact]
    public async Task GetPdf_IsDownloadableByBothParties()
    {
        var applicationId = await AcceptedFlatFeeAsync();

        foreach (var party in new[] { fixture.SeedState.VenueManager1, fixture.SeedState.ArtistManager1 })
        {
            var client = fixture.CreateClient(party);
            var response = await client.GetAsync($"/api/application/{applicationId}/contract/pdf");

            await response.ShouldBe(HttpStatusCode.OK);
            Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.NotEmpty(bytes);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        }
    }

    [Fact]
    public async Task GetPdf_Returns404ForNonParty()
    {
        var applicationId = await AcceptedFlatFeeAsync();
        var stranger = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await stranger.GetAsync($"/api/application/{applicationId}/contract/pdf");

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPdf_RendersOnFirstDownload()
    {
        var applicationId = await AcceptedFlatFeeAsync();
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var response = await client.GetAsync($"/api/application/{applicationId}/contract/pdf");

        await response.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPdf_RendersBothPartyESignatures()
    {
        var opportunityId = await CreateOpportunityAsync(
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var applyResponse = await artistClient.PostAsync(
            $"/api/application/{opportunityId}",
            new { eSignature = new { signatoryName = "Zola Banks" } });
        await applyResponse.ShouldBe(HttpStatusCode.Created);
        var application = await applyResponse.Content.ReadAsync<ApplicationBoundaryResponse>();
        Assert.NotNull(application);

        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{application.Id}/checkout");
        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{application.Id}/accept",
            new { eSignature = new { signatoryName = "Marco Vento" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        var response = await venueClient.GetAsync($"/api/application/{application.Id}/contract/pdf");
        await response.ShouldBe(HttpStatusCode.OK);
        var text = Pdf.ExtractText(await response.Content.ReadAsByteArrayAsync());

        Assert.Contains("Signatures", text);
        Assert.Contains("Signed by Zola Banks", text);
        Assert.Contains("Signed by Marco Vento", text);
        Assert.DoesNotContain("No recorded signature", text);
    }

    #endregion

    private async Task<int> AcceptedFlatFeeAsync()
    {
        var opportunityId = await CreateOpportunityAsync(
            new FlatFeeDealDto { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{applicationId}/checkout");
        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{applicationId}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        return applicationId;
    }

    private async Task<int> CreateOpportunityAsync(DealDto deal)
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var response = await venueClient.PostAsync("/api/opportunity", BuildOpportunityRequest(deal));
        await response.ShouldBe(HttpStatusCode.Created);
        var opportunity = await response.Content.ReadAsync<OpportunityBoundaryResponse>();
        Assert.NotNull(opportunity);
        return opportunity.Id;
    }

    private async Task<int> ApplyAsync(int opportunityId, string? paymentMethodId = null)
    {
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await artistClient.PostAsync(
            $"/api/application/{opportunityId}",
            new
            {
                eSignature = new { signatoryName = "Test Signatory" },
                paymentMethodId
            });
        await response.ShouldBe(HttpStatusCode.Created);
        var application = await response.Content.ReadAsync<ApplicationBoundaryResponse>();
        Assert.NotNull(application);
        return application.Id;
    }

    private async Task<ContractBoundaryResponse> GetContractAsync(int applicationId)
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var response = await client.GetAsync($"/api/application/{applicationId}/contract");
        await response.ShouldBe(HttpStatusCode.OK);
        var contract = await response.Content.ReadAsync<ContractBoundaryResponse>();
        Assert.NotNull(contract);
        return contract;
    }

    private void AssertCommonSnapshot(ContractBoundaryResponse contract)
    {
        Assert.NotEmpty(contract.VenueName);
        Assert.NotEmpty(contract.ArtistName);
        Assert.Equal("2026-07", contract.PlatformTermsVersion);
        Assert.NotEqual(default, contract.CreatedAtUtc);
        Assert.NotNull(contract.ArtistSignature);
        Assert.Equal(fixture.SeedState.ArtistManager1.Id, contract.ArtistSignature.UserId);
        Assert.NotEqual(default, contract.ArtistSignature.AtUtc);
        Assert.Equal("Test Signatory", contract.ArtistSignature.SignatoryName);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, contract.VenueSignature.UserId);
        Assert.NotEqual(default, contract.VenueSignature.AtUtc);
        Assert.Equal("Test Signatory", contract.VenueSignature.SignatoryName);
    }

    private async Task UpdateDealAsync(int opportunityId, DealDto desired)
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var currentResponse = await venueClient.GetAsync(
            $"/api/venue/{fixture.SeedState.Venue.Id}/opportunities");
        await currentResponse.ShouldBe(HttpStatusCode.OK);
        var current = await currentResponse.Content.ReadAsync<IReadOnlyList<OpportunityBoundaryResponse>>();
        Assert.NotNull(current);
        var requests = current
            .Select(opportunity => new OpportunityBoundaryRequest(
                opportunity.Id,
                opportunity.StartDate,
                opportunity.EndDate,
                opportunity.Genres,
                opportunity.Id == opportunityId ? desired : opportunity.Deal))
            .ToArray();

        var updateResponse = await venueClient.PutAsync(
            $"/api/venue/{fixture.SeedState.Venue.Id}/opportunities",
            requests);

        await updateResponse.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadAsync<IReadOnlyList<OpportunityBoundaryResponse>>();
        Assert.NotNull(updated);
        var target = Assert.Single(updated, opportunity => opportunity.Id == opportunityId);
        Assert.Equal(desired, target.Deal with { Id = desired.Id });
    }

    private OpportunityBoundaryRequest BuildOpportunityRequest(DealDto deal) =>
        new(
            null,
            fixture.SeedNow.AddMonths(1),
            fixture.SeedNow.AddMonths(1).AddHours(3),
            [Genre.Rock],
            deal);

    private sealed record ApplicationBoundaryResponse(int Id);

    private sealed record ContractBoundaryResponse(
        string VenueName,
        string ArtistName,
        DealType DealType,
        PaymentMethod PaymentMethod,
        string TermsText,
        string PlatformTermsVersion,
        SignatureBoundaryResponse ArtistSignature,
        SignatureBoundaryResponse VenueSignature,
        DateTime CreatedAtUtc);

    private sealed record SignatureBoundaryResponse(
        Guid UserId,
        DateTime AtUtc,
        string SignatoryName);

    private sealed record OpportunityBoundaryResponse(
        int Id,
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<Genre> Genres,
        DealDto Deal);

    private sealed record OpportunityBoundaryRequest(
        int? Id,
        DateTime StartDate,
        DateTime EndDate,
        IReadOnlyList<Genre> Genres,
        DealDto Deal);
}
