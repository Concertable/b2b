using System.Net;
using Concertable.B2B.Concert.Infrastructure.Emails;
using Concertable.Shared.Email.Application;
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
    public async Task Book_StagesBothPartiesLegalDetails_ToEveryMemberOfBothTenants()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");

        var accept = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await accept.ShouldBe(HttpStatusCode.NoContent);
        await fixture.StripeClient.SendWebhookAsync();

        var confirmations = (await fixture.GetStagedEmailsAsync())
            .Where(e => e.Subject.StartsWith("Booking confirmed:")).ToList();
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
    public void Render_ShowsLegalNameOnly_AndHtmlEscapes_WhenTaxComplianceAbsent()
    {
        using var scope = fixture.Services.CreateScope();
        var renderer = scope.ServiceProvider.GetRequiredService<IEmailRenderer>();

        var html = renderer.Render(new BookingConfirmationEmailContent(
            new EmailParty("The Venue", "Bar & Grill <Ltd>", null, null),
            new EmailParty("The Artist", "Artist Legal Name", null, null),
            "Monday 1 January 2035")).HtmlBody;

        Assert.Contains("Bar &amp; Grill &lt;Ltd&gt;", html);
        Assert.DoesNotContain("Bar & Grill <Ltd>", html);
        Assert.Contains("Artist Legal Name", html);
        Assert.DoesNotContain("VAT number", html);
        Assert.DoesNotContain("Seed Way", html);
    }
}
