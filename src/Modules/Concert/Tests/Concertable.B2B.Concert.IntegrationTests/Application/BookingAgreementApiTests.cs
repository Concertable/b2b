using System.Net;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { agreedToTerms = true });

        // Assert — snapshot written in the accept transaction
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(applicationId);
        Assert.Equal(ContractType.FlatFee, agreement.ContractType);
        Assert.Equal(PaymentMethod.Transfer, agreement.PaymentMethod);
        Assert.Equal(500m, agreement.Fee);
        Assert.Null(agreement.HireFee);
        Assert.Null(agreement.Guarantee);
        Assert.Null(agreement.ArtistDoorPercent);
        Assert.Equal("The venue pays the artist a flat fee of £500.00.", agreement.TermsText);
        AssertCommonSnapshot(agreement);

        // Act — the venue edits the live contract after acceptance
        await UpdateContractAsync(opportunityId, new FlatFeeContract { PaymentMethod = PaymentMethod.Cash, Fee = 999m });

        // Assert — the frozen agreement is untouched
        var frozen = await GetAgreementAsync(applicationId);
        Assert.Equal(500m, frozen.Fee);
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
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { agreedToTerms = true, paymentMethodId = "pm_card_visa" });

        // Assert
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(applicationId);
        Assert.Equal(ContractType.DoorSplit, agreement.ContractType);
        Assert.Equal(70m, agreement.ArtistDoorPercent);
        Assert.Null(agreement.Fee);
        Assert.Null(agreement.HireFee);
        Assert.Null(agreement.Guarantee);
        Assert.Equal("The artist receives 70% of door revenue.", agreement.TermsText);
        AssertCommonSnapshot(agreement);

        // Act
        await UpdateContractAsync(opportunityId, new DoorSplitContract { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 15m });

        // Assert
        var frozen = await GetAgreementAsync(applicationId);
        Assert.Equal(70m, frozen.ArtistDoorPercent);
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
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { agreedToTerms = true, paymentMethodId = "pm_card_visa" });

        // Assert
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(applicationId);
        Assert.Equal(ContractType.Versus, agreement.ContractType);
        Assert.Equal(200m, agreement.Guarantee);
        Assert.Equal(60m, agreement.ArtistDoorPercent);
        Assert.Null(agreement.Fee);
        Assert.Null(agreement.HireFee);
        Assert.Equal("The artist receives a guarantee of £200.00 plus 60% of door revenue.", agreement.TermsText);
        AssertCommonSnapshot(agreement);

        // Act
        await UpdateContractAsync(opportunityId, new VersusContract { PaymentMethod = PaymentMethod.Cash, Guarantee = 999m, ArtistDoorPercent = 10m });

        // Assert
        var frozen = await GetAgreementAsync(applicationId);
        Assert.Equal(200m, frozen.Guarantee);
        Assert.Equal(60m, frozen.ArtistDoorPercent);
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
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { agreedToTerms = true });

        // Assert
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(applicationId);
        Assert.Equal(ContractType.VenueHire, agreement.ContractType);
        Assert.Equal(250m, agreement.HireFee);
        Assert.Null(agreement.Fee);
        Assert.Null(agreement.Guarantee);
        Assert.Null(agreement.ArtistDoorPercent);
        Assert.Equal("The artist pays the venue a hire fee of £250.00.", agreement.TermsText);
        AssertCommonSnapshot(agreement);

        // Act
        await UpdateContractAsync(opportunityId, new VenueHireContract { PaymentMethod = PaymentMethod.Cash, HireFee = 999m });

        // Assert
        var frozen = await GetAgreementAsync(applicationId);
        Assert.Equal(250m, frozen.HireFee);
        Assert.Equal("The artist pays the venue a hire fee of £250.00.", frozen.TermsText);
    }

    [Fact]
    public async Task Apply_ShouldReturn400_WithoutConsent()
    {
        // Arrange
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var artistClient = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await artistClient.PostAsync($"/api/Application/{opportunityId}", new { agreedToTerms = false });

        // Assert — no application row written
        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.False(await fixture.ConcertReads.Set<ApplicationEntity>().AnyAsync(a => a.OpportunityId == opportunityId));
    }

    [Fact]
    public async Task Apply_ShouldRecordArtistConsentAndFingerprint()
    {
        // Arrange + Act
        var opportunityId = await CreateOpportunityAsync(new FlatFeeContract { PaymentMethod = PaymentMethod.Transfer, Fee = 500m });
        var applicationId = await ApplyAsync(opportunityId);

        // Assert
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == applicationId);
        Assert.NotNull(application.ArtistConsent);
        Assert.Equal(fixture.SeedState.ArtistManager1.Id, application.ArtistConsent!.UserId);
        Assert.NotEqual(default, application.ArtistConsent.AtUtc);
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
        var response = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { agreedToTerms = false });

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
        var response = await venueClient.PostAsync($"/api/Application/{applicationId}/accept", new { agreedToTerms = true });

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
        var acceptResponse = await venueClient.PostAsync($"/api/Application/{appId}/accept", new { agreedToTerms = true });

        // Assert — accept works, agreement records the venue's consent and honestly omits the artist's
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);
        var agreement = await GetAgreementAsync(appId);
        Assert.Null(agreement.ArtistConsent);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, agreement.VenueConsent.UserId);
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
            $"/api/Application/{opportunityId}", new { agreedToTerms = true, paymentMethodId });
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
        Assert.NotNull(agreement.ArtistConsent);
        Assert.Equal(fixture.SeedState.ArtistManager1.Id, agreement.ArtistConsent!.UserId);
        Assert.NotEqual(default, agreement.ArtistConsent.AtUtc);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, agreement.VenueConsent.UserId);
        Assert.NotEqual(default, agreement.VenueConsent.AtUtc);
        Assert.Null(agreement.PdfBlobName);
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
