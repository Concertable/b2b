using System.Net;
using Xunit.Abstractions;

namespace Concertable.B2B.Lifecycle.IntegrationTests;

[Collection("Integration")]
public sealed class BookingConfirmationEmailLifecycleTests : IAsyncLifetime
{
    private readonly LifecycleApiFixture fixture;

    public BookingConfirmationEmailLifecycleTests(LifecycleApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Book_StagesBothPartiesLegalDetails_ToEveryMemberOfBothTenants()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        var accept = await client.PostAsync(
            $"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await accept.ShouldBe(HttpStatusCode.NoContent);
        await fixture.PaymentSimulator.SendWebhookAsync();

        var confirmations = (await fixture.GetStagedEmailsAsync())
            .Where(email => email.Subject.StartsWith("Booking confirmed:", StringComparison.Ordinal))
            .ToList();
        var recipients = confirmations.Select(email => email.To).ToList();
        var venueRegisteredAddress = fixture.SeedState.Tenants
            .Single(tenant => tenant.Id == fixture.SeedState.Venue.TenantId)
            .TaxCompliance?
            .RegisteredAddress;
        var artistRegisteredAddress = fixture.SeedState.Tenants
            .Single(tenant => tenant.Id == fixture.SeedState.Artist.TenantId)
            .TaxCompliance?
            .RegisteredAddress;
        Assert.NotNull(venueRegisteredAddress);
        Assert.NotNull(artistRegisteredAddress);
        var venueAddress = FormatAddress(
            venueRegisteredAddress.Line1,
            venueRegisteredAddress.Line2,
            venueRegisteredAddress.City,
            venueRegisteredAddress.Postcode,
            venueRegisteredAddress.Country);
        var artistAddress = FormatAddress(
            artistRegisteredAddress.Line1,
            artistRegisteredAddress.Line2,
            artistRegisteredAddress.City,
            artistRegisteredAddress.Postcode,
            artistRegisteredAddress.Country);

        Assert.Contains(fixture.SeedState.VenueManager1.Email, recipients);
        Assert.Contains(fixture.SeedState.VenueManager3.Email, recipients);
        Assert.Contains(fixture.SeedState.ArtistManager1.Email, recipients);
        Assert.NotEmpty(confirmations);
        Assert.All(confirmations, email =>
        {
            Assert.Contains(fixture.SeedState.VenueManager1.Email, email.Body);
            Assert.Contains(fixture.SeedState.ArtistManager1.Email, email.Body);
            Assert.Contains(venueAddress, email.Body);
            Assert.Contains(artistAddress, email.Body);
        });
    }

    private static string FormatAddress(params string?[] parts) =>
        string.Join(", ", parts.Where(value => !string.IsNullOrWhiteSpace(value)));
}
