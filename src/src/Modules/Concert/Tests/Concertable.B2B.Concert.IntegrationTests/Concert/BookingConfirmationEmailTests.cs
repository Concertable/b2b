using System.Net;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class BookingConfirmationEmailTests : IAsyncLifetime
{
    private const string SeededAddress = "1 Seed Way, London, EC1A 1AA, United Kingdom";

    private readonly ConcertApiFixture fixture;

    public BookingConfirmationEmailTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Book_SendsBothPartiesLegalDetails_ToEveryMemberOfBothTenants()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        var accept = await client.PostAsync($"/api/Application/{fixture.SeedState.FlatFeeApp.Id}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await accept.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        var confirmations = fixture.EmailSender.Sent.Where(e => e.Subject.StartsWith("Booking confirmed:")).ToList();
        var recipients = confirmations.Select(e => e.To).ToList();

        // Venue tenant has two members (VenueManager1 + VenueManager3); artist tenant has one.
        Assert.Contains(fixture.SeedState.VenueManager1.Email, recipients);
        Assert.Contains(fixture.SeedState.VenueManager3.Email, recipients);
        Assert.Contains(fixture.SeedState.ArtistManager1.Email, recipients);

        Assert.NotEmpty(confirmations);
        Assert.All(confirmations, e =>
        {
            Assert.Contains(fixture.SeedState.VenueManager1.Email, e.Body);
            Assert.Contains(fixture.SeedState.ArtistManager1.Email, e.Body);
            Assert.Contains(SeededAddress, e.Body);
        });
    }

    [Fact]
    public async Task Book_RendersLegalNameOnly_WhenTenantTaxComplianceAbsent()
    {
        var venueTenant = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.VenueManagerNoVenue.Id);
        var artistTenant = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == fixture.SeedState.ArtistManagerNoArtist.Id);
        var period = new DateRange(fixture.SeedNow.AddDays(30), fixture.SeedNow.AddDays(30).AddHours(3));

        using var scope = fixture.Services.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<IBookingConfirmationNotifier>();

        await notifier.BookingConfirmedAsync(venueTenant.Id, "Test Venue", artistTenant.Id, "Test Artist", period);

        var confirmations = fixture.EmailSender.Sent.Where(e => e.Subject.StartsWith("Booking confirmed:")).ToList();
        var recipients = confirmations.Select(e => e.To).ToList();

        Assert.Contains(fixture.SeedState.VenueManagerNoVenue.Email, recipients);
        Assert.Contains(fixture.SeedState.ArtistManagerNoArtist.Email, recipients);

        Assert.NotEmpty(confirmations);
        Assert.All(confirmations, e =>
        {
            Assert.Contains(fixture.SeedState.VenueManagerNoVenue.Email, e.Body);
            Assert.Contains(fixture.SeedState.ArtistManagerNoArtist.Email, e.Body);
            Assert.DoesNotContain("VAT number", e.Body);
            Assert.DoesNotContain("Seed Way", e.Body);
        });
    }
}
