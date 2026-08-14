using System.Net;
using System.Text;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

/// <summary>
/// The supplier-facing self-billing agreement surface (<c>SelfBillingAgreementController</c>): grant/renew with an
/// e-signature, read own status, and download own PDF — single-owner scoped, reachable by both tenant types. The
/// dev/E2E seeder grant runs only under <c>IDevSeeder</c>, so under the test seeder every tenant starts with no
/// agreement, letting each affordance state (none/active/expired/nearing) be driven exactly.
/// </summary>
[Collection("Integration")]
public sealed class SelfBillingAgreementApiTests : IAsyncLifetime
{
    private const string Path = "/api/self-billing-agreement";

    private readonly ConcertApiFixture fixture;

    public SelfBillingAgreementApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Get_ReturnsNone_WithGrantAffordance_WhenNeverGranted()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync(Path);

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsync<SelfBillingAgreementResponse>();
        Assert.Equal(SelfBillingAgreementStatus.None, body!.Status);
        Assert.NotNull(body.Actions.Grant);
        Assert.Equal(Path, body.Actions.Grant!.Href);
        Assert.Equal("POST", body.Actions.Grant.Method);
        Assert.Null(body.Actions.Renew);
        Assert.Null(body.Actions.Pdf);
    }

    [Fact]
    public async Task Grant_RecordsSupplierESignature_AndBecomesActive()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var grant = await client.PostAsync(Path, new { eSignature = new { signatoryName = "Vince Venue" } });

        await grant.ShouldBe(HttpStatusCode.NoContent);
        var tenantId = TenantIdOf(fixture.SeedState.VenueManager1.Id);
        var row = await fixture.ConcertReads.Set<SelfBillingAgreementEntity>().SingleAsync(a => a.TenantId == tenantId);
        Assert.Equal(fixture.SeedState.VenueManager1.Id, row.SupplierESignature.UserId);
        Assert.Equal("Vince Venue", row.SupplierESignature.SignatoryName);

        var status = await ReadStatusAsync(client);
        Assert.Equal(SelfBillingAgreementStatus.Active, status.Status);
        Assert.NotNull(status.Actions.Pdf);
        Assert.Null(status.Actions.Grant);
        Assert.Null(status.Actions.Renew); // a fresh 12-month window is not yet within the renewal window
    }

    [Fact]
    public async Task Grant_Returns400_WithoutConsent()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.PostAsync(Path, new { eSignature = new { signatoryName = "" } });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var tenantId = TenantIdOf(fixture.SeedState.VenueManager1.Id);
        Assert.False(await fixture.ConcertReads.Set<SelfBillingAgreementEntity>().AnyAsync(a => a.TenantId == tenantId));
    }

    [Fact]
    public async Task Grant_IsReachableByBothArtistAndVenueTenants()
    {
        foreach (var supplier in new[] { fixture.SeedState.VenueManager1, fixture.SeedState.ArtistManager1 })
        {
            var client = fixture.CreateClient(supplier);
            var response = await client.PostAsync(Path, new { eSignature = new { signatoryName = "Supplier" } });
            await response.ShouldBe(HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task Renew_BeforeExpiry_OffersRenewAffordance_AndAppendsAcceptance()
    {
        var tenantId = TenantIdOf(fixture.SeedState.VenueManager1.Id);
        await InsertAgreementAsync(tenantId, AppNowUtc().AddDays(-350)); // in force, but within the 30-day renewal window
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var status = await ReadStatusAsync(client);
        Assert.Equal(SelfBillingAgreementStatus.Active, status.Status);
        Assert.NotNull(status.Actions.Renew);
        Assert.Null(status.Actions.Grant);

        var renew = await client.PostAsync(Path, new { eSignature = new { signatoryName = "Vince Venue" } });
        await renew.ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(2, await fixture.ConcertReads.Set<SelfBillingAgreementEntity>().CountAsync(a => a.TenantId == tenantId));
    }

    [Fact]
    public async Task Renew_AfterExpiry_FlipsFromExpiredToActive()
    {
        var tenantId = TenantIdOf(fixture.SeedState.VenueManager1.Id);
        await InsertAgreementAsync(tenantId, AppNowUtc().AddMonths(-13)); // lapsed
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var expired = await ReadStatusAsync(client);
        Assert.Equal(SelfBillingAgreementStatus.Expired, expired.Status);
        Assert.NotNull(expired.Actions.Renew);
        Assert.Null(expired.Actions.Grant);
        Assert.Null(expired.Actions.Pdf); // nothing in force to download

        await (await client.PostAsync(Path, new { eSignature = new { signatoryName = "Vince Venue" } })).ShouldBe(HttpStatusCode.NoContent);

        var active = await ReadStatusAsync(client);
        Assert.Equal(SelfBillingAgreementStatus.Active, active.Status);
        Assert.NotNull(active.Actions.Pdf);
    }

    [Fact]
    public async Task Pdf_IsDownloadableByOwner_AfterGrant()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await (await client.PostAsync(Path, new { eSignature = new { signatoryName = "Vince Venue" } })).ShouldBe(HttpStatusCode.NoContent);

        var response = await client.GetAsync($"{Path}/pdf");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4)); // the PDF magic number
    }

    [Fact]
    public async Task Pdf_Returns404_ForTenantWithoutAgreement()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await client.GetAsync($"{Path}/pdf");

        // Single-owner: a caller can only ever fetch its own, and it holds none — 404, never a probe-able 403.
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<SelfBillingAgreementResponse> ReadStatusAsync(HttpClient client)
    {
        var response = await client.GetAsync(Path);
        await response.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadAsync<SelfBillingAgreementResponse>())!;
    }

    private Guid TenantIdOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    private DateTime AppNowUtc()
    {
        using var scope = fixture.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
    }

    // A host (no-HTTP) scope, so the tenant interceptor no-ops and the row keeps the explicit supplier TenantId.
    private async Task InsertAgreementAsync(Guid tenantId, DateTime acceptedAtUtc)
    {
        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConcertTenantDbContext>();
        context.SelfBillingAgreements.Add(SelfBillingAgreementEntity.Create(
            tenantId,
            new InvoiceParty(tenantId, "Sally Supplier Ltd", "GB123456789", "1 Road", null, "Town", "AB1 2CD", "United Kingdom"),
            new ESignature(Guid.NewGuid(), acceptedAtUtc, IPAddress.Loopback, "supplier-agent", "Sally Supplier", null),
            "This self-billing agreement authorises self-billed invoices.",
            "2026-07",
            acceptedAtUtc,
            acceptedAtUtc));
        await context.SaveChangesAsync();
    }
}
