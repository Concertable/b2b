using System.Net;
using Xunit.Abstractions;

namespace Concertable.B2B.Process.IntegrationTests;

[Collection("Integration")]
public sealed class BookingConfirmationEmailJourneyTests : IAsyncLifetime
{
    private readonly ProcessApiFixture fixture;

    public BookingConfirmationEmailJourneyTests(ProcessApiFixture fixture, ITestOutputHelper output)
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
        await fixture.StripeClient.SendWebhookAsync();

        var confirmations = (await fixture.GetStagedEmailsAsync())
            .Where(email => email.Subject.StartsWith("Booking confirmed:", StringComparison.Ordinal))
            .ToList();
        var recipients = confirmations.Select(email => email.To).ToList();
        var venueAddress = fixture.SeedState.Tenants
            .Single(tenant => tenant.Id == fixture.SeedState.Venue.TenantId)
            .RegisteredAddress;
        var artistAddress = fixture.SeedState.Tenants
            .Single(tenant => tenant.Id == fixture.SeedState.Artist.TenantId)
            .RegisteredAddress;
        Assert.NotNull(venueAddress);
        Assert.NotNull(artistAddress);

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
}
