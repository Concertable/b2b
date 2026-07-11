using System.Net;
using System.Text;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Xunit;
using Xunit.Abstractions;
using static Concertable.B2B.Concert.IntegrationTests.Opportunity.OpportunityRequestBuilders;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class BookingAgreementApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public BookingAgreementApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Accept_ShouldSnapshotAgreement_ThatSurvivesContractEdit_ForFlatFee()
    {
        // Arrange — fresh FlatFee opportunity, artist applies, venue checkout + accept
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{applicationId}/checkout");

        // Act
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert — snapshot written in the accept transaction
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(applicationId);
        Assert.Equal(ContractType.FlatFee, agreement.ContractType);
        Assert.Equal(PaymentMethod.Transfer, agreement.PaymentMethod);
        Assert.Equal("The venue pays the artist a flat fee of £500.00.", agreement.TermsText);
        AssertCommonSnapshot(agreement);

        // Act — the venue edits the live contract after acceptance
        await UpdateContractAsync(opportunityId, new FlatFeeContract { PaymentMethod = PaymentMethod.Cash, Fee = 999m });

        // Assert — the frozen agreement is untouched (the terms text still states the agreed £500)
        var frozen = await GetAgreementAsync(applicationId);
        Assert.Equal(PaymentMethod.Transfer, frozen.PaymentMethod);
        Assert.Equal("The venue pays the artist a flat fee of £500.00.", frozen.TermsText);
    }

    [Fact]
    public async Task Accept_ShouldSnapshotAgreement_ThatSurvivesContractEdit_ForDoorSplit()
    {
        // Arrange
        var opportunityId = await CreateOpportunityAsync(new DoorSplitContract { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { eSignature = new { signatoryName = "Test Signatory" }, paymentMethodId = "pm_card_visa" });

        // Assert
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(applicationId);
        Assert.Equal(ContractType.DoorSplit, agreement.ContractType);
        Assert.Equal("The artist receives 70% of door revenue.", agreement.TermsText);
        AssertCommonSnapshot(agreement);

        // Act
        await UpdateContractAsync(opportunityId, new DoorSplitContract { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 15m });

        // Assert — the frozen terms text still states the agreed 70%
        var frozen = await GetAgreementAsync(applicationId);
        Assert.Equal("The artist receives 70% of door revenue.", frozen.TermsText);
    }

    [Fact]
    public async Task Accept_ShouldSnapshotAgreement_ThatSurvivesContractEdit_ForVersus()
    {
        // Arrange
        var opportunityId = await CreateOpportunityAsync(new VersusContract { PaymentMethod = PaymentMethod.Cash, Guarantee = 200m, ArtistDoorPercent = 60m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { eSignature = new { signatoryName = "Test Signatory" }, paymentMethodId = "pm_card_visa" });

        // Assert
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(applicationId);
        Assert.Equal(ContractType.Versus, agreement.ContractType);
        Assert.Equal("The artist receives a guarantee of £200.00 plus 60% of door revenue.", agreement.TermsText);
        AssertCommonSnapshot(agreement);

        // Act
        await UpdateContractAsync(opportunityId, new VersusContract { PaymentMethod = PaymentMethod.Cash, Guarantee = 999m, ArtistDoorPercent = 10m });

        // Assert — the frozen terms text still states the agreed £200 guarantee + 60%
        var frozen = await GetAgreementAsync(applicationId);
        Assert.Equal("The artist receives a guarantee of £200.00 plus 60% of door revenue.", frozen.TermsText);
    }

    [Fact]
    public async Task Accept_ShouldSnapshotAgreement_ThatSurvivesContractEdit_ForVenueHire()
    {
        // Arrange — VenueHire is prepaid: the artist applies with a payment method
        var opportunityId = await CreateOpportunityAsync(new VenueHireContract { PaymentMethod = PaymentMethod.Cash, HireFee = 250m });
        var applicationId = await ApplyAsync(opportunityId, "pm_card_visa");
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(applicationId);
        Assert.Equal(ContractType.VenueHire, agreement.ContractType);
        Assert.Equal("The artist pays the venue a hire fee of £250.00.", agreement.TermsText);
        AssertCommonSnapshot(agreement);

        // Act
        await UpdateContractAsync(opportunityId, new VenueHireContract { PaymentMethod = PaymentMethod.Cash, HireFee = 999m });

        // Assert — the frozen terms text still states the agreed £250
        var frozen = await GetAgreementAsync(applicationId);
        Assert.Equal("The artist pays the venue a hire fee of £250.00.", frozen.TermsText);
    }

    [Fact]
    public async Task Apply_ShouldReturn400_WithoutConsent()
    {
        // Arrange
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await artistClient.PostAsync($"/api/Application/{opportunityId}", new { eSignature = new { signatoryName = "" } });

        // Assert — no application row written
        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.False(await fixture.ConcertReads.Set<ApplicationEntity>().AnyAsync(a => a.OpportunityId == opportunityId));
    }

    [Fact]
    public async Task Apply_ShouldRecordArtistESignatureAndFingerprint()
    {
        // Arrange + Act
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);

        // Assert
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == applicationId);
        Assert.NotNull(application.ArtistESignature);
        Assert.Equal(fixture.SeedState.ArtistManager1.Id, application.ArtistESignature!.UserId);
        Assert.NotEqual(default, application.ArtistESignature.AtUtc);
        Assert.Equal("Test Signatory", application.ArtistESignature.SignatoryName);
        Assert.NotNull(application.TermsFingerprint);
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WithoutConsent()
    {
        // Arrange
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{applicationId}/checkout");

        // Act
        var response = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { eSignature = new { signatoryName = "" } });

        // Assert — the accept never happened
        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.False(await fixture.ConcertReads.Set<BookingEntity>().AnyAsync(b => b.ApplicationId == applicationId));
    }

    [Fact]
    public async Task Accept_ShouldReturn400_WhenTermsChangedSinceApply()
    {
        // Arrange — artist consents to £500, then the venue edits the live contract to £999
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        await UpdateContractAsync(opportunityId, new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 999m });
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{applicationId}/checkout");

        // Act
        var response = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert — accept refused; no booking, no agreement
        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.False(await fixture.ConcertReads.Set<BookingEntity>().AnyAsync(b => b.ApplicationId == applicationId));
    }

    [Fact]
    public async Task Accept_ShouldSucceedWithNullArtistConsent_ForPreConsentApplication()
    {
        // Arrange — seeded applications predate click-wrap: no fingerprint, no artist consent
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{appId}/checkout");

        // Act
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert — accept works, agreement records the venue's consent and honestly omits the artist's
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(appId);
        Assert.Null(agreement.ArtistESignature);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, agreement.VenueESignature.UserId);
    }

    [Fact]
    public async Task Agreement_Pdf_IsDownloadableByBothParties()
    {
        var applicationId = await AcceptedFlatFeeAsync();

        foreach (var party in new[] { fixture.SeedState.VenueManager1, fixture.SeedState.ArtistManager1 })
        {
            var client = fixture.CreateClient(party);
            var response = await client.GetAsync($"/api/Application/{applicationId}/agreement/pdf");

            await response.ShouldBe(HttpStatusCode.OK);
            Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.NotEmpty(bytes);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4)); // the PDF magic number
        }
    }

    [Fact]
    public async Task Agreement_Pdf_Returns404ForNonParty()
    {
        var applicationId = await AcceptedFlatFeeAsync();

        var stranger = fixture.CreateClient(fixture.SeedState.VenueManager2);
        var response = await stranger.GetAsync($"/api/Application/{applicationId}/agreement/pdf");

        // The two-party filter hides the deal document — 404, never a probe-able 403.
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Agreement_Pdf_BlobNameAssignedAtAccept_AndRendersOnDownload()
    {
        var applicationId = await AcceptedFlatFeeAsync();

        // The blob location is assigned inside the accept transaction, before any bytes exist — so it
        // is present immediately under the agreements/ prefix, independent of background PDF timing.
        var agreement = await GetAgreementAsync(applicationId);
        Assert.NotNull(agreement.PdfBlobName);
        Assert.StartsWith("agreements/", agreement.PdfBlobName);

        // FakeBlobStorageService reports the blob absent, so the download exercises the lazy render-
        // on-download path and still returns the PDF.
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var response = await client.GetAsync($"/api/Application/{applicationId}/agreement/pdf");
        await response.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Agreement_Pdf_RendersBothPartyESignatures()
    {
        // Distinct names so each extracted signature line ties to its party — artist signs on apply, venue on accept.
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        await (await artistClient.PostAsync($"/api/Application/{opportunityId}", new { eSignature = new { signatoryName = "Zola Banks" } })).ShouldBe(HttpStatusCode.Created);
        var applicationId = (await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.OpportunityId == opportunityId)).Id;

        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{applicationId}/checkout");
        await (await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { eSignature = new { signatoryName = "Marco Vento" } })).ShouldBe(HttpStatusCode.NoContent);

        var response = await venueClient.GetAsync($"/api/Application/{applicationId}/agreement/pdf");
        await response.ShouldBe(HttpStatusCode.OK);
        var text = ExtractPdfText(await response.Content.ReadAsByteArrayAsync());

        Assert.Contains("Signatures", text);
        Assert.Contains("Signed by Zola Banks", text);
        Assert.Contains("Signed by Marco Vento", text);
        Assert.DoesNotContain("No recorded signature", text);
    }

    [Fact]
    public async Task Agreement_Pdf_RendersPlaceholder_ForPreESignArtistSignature()
    {
        // The seeded FlatFeeApp predates e-sign: no artist signature. The venue still signs at accept,
        // so the PDF shows the venue's signature and the placeholder for the artist — never a fabricated one.
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{appId}/checkout");
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{appId}/accept", new { eSignature = new { signatoryName = "Marco Vento" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        var response = await venueClient.GetAsync($"/api/Application/{appId}/agreement/pdf");
        await response.ShouldBe(HttpStatusCode.OK);
        var text = ExtractPdfText(await response.Content.ReadAsByteArrayAsync());

        Assert.Contains("Signatures", text);
        Assert.Contains("No recorded signature (predates e-sign)", text);
        Assert.Contains("Signed by Marco Vento", text);
    }

    private static string ExtractPdfText(byte[] bytes)
    {
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        using var pdf = PdfDocument.Open(bytes);
        var raw = string.Join(" ", pdf.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));
        return Regex.Replace(raw, @"\s+", " ");
    }

    [Fact]
    public async Task Agreement_Metadata_IsReadableByParty_And404ForStranger()
    {
        var applicationId = await AcceptedFlatFeeAsync();

        var artist = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await artist.GetAsync($"/api/Application/{applicationId}/agreement");
        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("The venue pays the artist a flat fee of", body);
        Assert.Contains("2026-07", body); // platform terms version

        var stranger = fixture.CreateClient(fixture.SeedState.VenueManager2);
        await (await stranger.GetAsync($"/api/Application/{applicationId}/agreement")).ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptedApplication_ExposesAgreementHateoasLink()
    {
        var applicationId = await AcceptedFlatFeeAsync();

        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var response = await venue.GetAsync($"/api/Application/{applicationId}");
        await response.ShouldBe(HttpStatusCode.OK);
        var application = await response.Content.ReadAsync<ApplicationResponse>();

        Assert.NotNull(application!.Actions.Agreement);
        Assert.Equal($"/api/Application/{applicationId}/agreement", application.Actions.Agreement!.Href);
        Assert.Equal("GET", application.Actions.Agreement.Method);
    }

    [Fact]
    public async Task PendingApplication_HasNoAgreementLink()
    {
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);

        var artist = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await artist.GetAsync($"/api/Application/{applicationId}");
        await response.ShouldBe(HttpStatusCode.OK);
        var application = await response.Content.ReadAsync<ApplicationResponse>();

        Assert.Null(application!.Actions.Agreement);
    }

    private async Task<int> AcceptedFlatFeeAsync()
    {
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/Application/{applicationId}/checkout");
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        return applicationId;
    }

    private async Task<int> CreateOpportunityAsync(IContract contract)
    {
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var response = await venueClient.PostAsync("/api/Opportunity", BuildRequest(contract));
        await response.ShouldBe(HttpStatusCode.Created);
        var opportunity = await response.Content.ReadAsync<OpportunityResponse>();
        return opportunity!.Id;
    }

    private async Task<int> ApplyAsync(int opportunityId, string? paymentMethodId = null)
    {
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await artistClient.PostAsync(
            $"/api/Application/{opportunityId}", new { eSignature = new { signatoryName = "Test Signatory" }, paymentMethodId });
        await response.ShouldBe(HttpStatusCode.Created);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>()
            .FirstAsync(a => a.OpportunityId == opportunityId);
        return application.Id;
    }

    private async Task<BookingAgreementEntity> GetAgreementAsync(int applicationId)
    {
        var booking = await fixture.ConcertReads.Set<BookingEntity>()
            .FirstAsync(b => b.ApplicationId == applicationId);
        var agreement = await fixture.ConcertReads.Set<BookingAgreementEntity>()
            .SingleAsync(a => a.BookingId == booking.Id);
        Assert.Equal(booking.VenueTenantId, agreement.VenueTenantId);
        Assert.Equal(booking.ArtistTenantId, agreement.ArtistTenantId);
        return agreement;
    }

    private void AssertCommonSnapshot(BookingAgreementEntity agreement)
    {
        Assert.NotEmpty(agreement.VenueName);
        Assert.NotEmpty(agreement.ArtistName);
        Assert.Equal("2026-07", agreement.PlatformTermsVersion);
        Assert.NotEqual(default, agreement.CreatedAtUtc);
        Assert.NotNull(agreement.ArtistESignature);
        Assert.Equal(fixture.SeedState.ArtistManager1.Id, agreement.ArtistESignature!.UserId);
        Assert.NotEqual(default, agreement.ArtistESignature.AtUtc);
        Assert.Equal("Test Signatory", agreement.ArtistESignature.SignatoryName);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, agreement.VenueESignature.UserId);
        Assert.NotEqual(default, agreement.VenueESignature.AtUtc);
        Assert.Equal("Test Signatory", agreement.VenueESignature.SignatoryName);
        // PdfBlobName is intentionally not asserted here: Phase 3 generates the PDF in a background
        // task at Accept, so it becomes populated shortly after — racy to assert either way. The PDF
        // lifecycle is covered by the dedicated Agreement_Pdf_* tests.
    }

    // The live edit venues make through OpportunitySyncer.UpdateAsync — mutates the contract row in place.
    private async Task UpdateContractAsync(int opportunityId, IContract desired)
    {
        var opportunity = await fixture.ConcertReads.Set<OpportunityEntity>()
            .FirstAsync(o => o.Id == opportunityId);

        using var scope = fixture.Services.CreateScope();
        var contracts = scope.ServiceProvider.GetRequiredService<IContractModule>();
        await contracts.UpdateAsync(opportunity.ContractId, desired);

        var updated = await contracts.GetByIdAsync(opportunity.ContractId);
        desired.Id = opportunity.ContractId;
        Assert.Equal(desired, updated); // sanity: the live contract really changed
    }
}
