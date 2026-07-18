using System.Net;
using System.Text;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.User.Domain;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

/// <summary>
/// The self-billed VAT invoice minted at settlement (<c>IInvoiceIssuer</c>, inside the Finish transaction).
/// Asserts the supply direction (supplier = settlement payee, customer = its inverse), the VAT decomposition
/// (unregistered supplier ⇒ no VAT; registered ⇒ inclusive gross decomposed), gap-free per-supplier
/// numbering, and that a deferred settlement mints nothing. Amounts are the seeded Past* bookings' fees.
/// </summary>
[Collection("Integration")]
public sealed class ConcertInvoiceApiTests : IAsyncLifetime
{
    private const decimal DoorRevenue = 200m;

    private readonly ConcertApiFixture fixture;

    public ConcertInvoiceApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Task<InvoiceEntity?> InvoiceForBookingAsync(int bookingId) =>
        fixture.ConcertReads.Set<InvoiceEntity>().FirstOrDefaultAsync(i => i.BookingId == bookingId);

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    private async Task RepointArtistTenantAsync(int concertId, Guid artistTenantId)
    {
        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
        await context.Concerts.Where(c => c.Id == concertId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ArtistTenantId, artistTenantId));
    }

    // The write validator only runs on the org-setup HTTP path; here we set the raw column so a seeded
    // (VAT-null) supplier becomes registered. VatPolicy keys off presence, not format, so any value registers.
    private Task SetVatNumberAsync(Guid tenantId, string vatNumber) =>
        fixture.ConcertReads.Database.ExecuteSqlRawAsync(
            "UPDATE [tenant].[Tenants] SET TaxCompliance_VatNumber = {0} WHERE Id = {1}", vatNumber, tenantId);

    // --- Direction + no-VAT (unregistered seed suppliers) ---

    [Fact]
    public async Task Finish_FlatFee_MintsInvoice_ArtistSupplierVenueCustomer_NoVatWhenUnregistered()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;

        await fixture.FinishConcertAsync(concert.Id);

        var invoice = await InvoiceForBookingAsync(booking.Id);
        Assert.NotNull(invoice);
        Assert.Equal(DealType.FlatFee, invoice!.DealType);
        Assert.Equal(concert.ArtistTenantId, invoice.Supplier.TenantId);
        Assert.Equal(concert.VenueTenantId, invoice.Customer.TenantId);
        Assert.Equal(200m, invoice.Amounts.Gross);
        Assert.Equal(200m, invoice.Amounts.Net);
        Assert.Equal(0m, invoice.Amounts.Vat);
        Assert.Equal(0m, invoice.Amounts.Rate);
        Assert.Equal(concert.Period.End, invoice.TaxPointUtc);
        Assert.Equal("INV-SEED000001-000001", invoice.InvoiceNumber);
    }

    [Fact]
    public async Task Finish_VenueHire_MintsInvoice_VenueSupplierArtistCustomer()
    {
        var booking = fixture.SeedState.PastVenueHireBooking;
        var concert = booking.Concert!;

        await fixture.FinishConcertAsync(concert.Id);

        var invoice = await InvoiceForBookingAsync(booking.Id);
        Assert.NotNull(invoice);
        Assert.Equal(DealType.VenueHire, invoice!.DealType);
        Assert.Equal(concert.VenueTenantId, invoice.Supplier.TenantId);   // direction flip: the venue is the supplier
        Assert.Equal(concert.ArtistTenantId, invoice.Customer.TenantId);
        Assert.Equal(300m, invoice.Amounts.Gross);
        Assert.Equal(300m, invoice.Amounts.Net);
        Assert.Equal(0m, invoice.Amounts.Vat);
    }

    [Fact]
    public async Task Finish_DoorSplit_MintsInvoice_ArtistSupplier_GrossMatchesPayout()
    {
        var booking = fixture.SeedState.PastDoorSplitBooking;
        var concert = booking.Concert!;
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        await fixture.FinishConcertAsync(concert.Id);

        var invoice = await InvoiceForBookingAsync(booking.Id);
        Assert.NotNull(invoice);
        Assert.Equal(DealType.DoorSplit, invoice!.DealType);
        Assert.Equal(concert.ArtistTenantId, invoice.Supplier.TenantId);
        Assert.Equal(concert.VenueTenantId, invoice.Customer.TenantId);
        Assert.Equal(0m, invoice.Amounts.Vat);
        Assert.Equal(invoice.Amounts.Gross, invoice.Amounts.Net);

        // charged == invoiced: the invoice gross is the exact share the payout step paid, via the shared resolver.
        var payout = fixture.ManagerPaymentClient.Payments.Single(p => p.BookingId == booking.Id);
        Assert.Equal(payout.Amount, invoice.Amounts.Gross);
    }

    [Fact]
    public async Task Finish_Versus_MintsInvoice_ArtistSupplier_GrossMatchesPayout()
    {
        var booking = fixture.SeedState.PastVersusBooking;
        var concert = booking.Concert!;
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        await fixture.FinishConcertAsync(concert.Id);

        var invoice = await InvoiceForBookingAsync(booking.Id);
        Assert.NotNull(invoice);
        Assert.Equal(DealType.Versus, invoice!.DealType);
        Assert.Equal(concert.ArtistTenantId, invoice.Supplier.TenantId);

        var payout = fixture.ManagerPaymentClient.Payments.Single(p => p.BookingId == booking.Id);
        Assert.Equal(payout.Amount, invoice.Amounts.Gross);
    }

    // --- VAT decomposition when the supplier is registered ---

    [Fact]
    public async Task Finish_FlatFee_RegisteredArtist_DecomposesInclusiveVat()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;
        await SetVatNumberAsync(concert.ArtistTenantId, "GB123456789");

        await fixture.FinishConcertAsync(concert.Id);

        var invoice = await InvoiceForBookingAsync(booking.Id);
        Assert.NotNull(invoice);
        Assert.Equal(200m, invoice!.Amounts.Gross);   // £200 inclusive, decomposed (not inflated)
        Assert.Equal(166.67m, invoice.Amounts.Net);   // round(200 / 1.20, 2)
        Assert.Equal(33.33m, invoice.Amounts.Vat);    // gross - net
        Assert.Equal(0.20m, invoice.Amounts.Rate);
        Assert.Equal("GB123456789", invoice.Supplier.VatNumber);
    }

    // --- Fail-closed: a deferred settlement mints nothing ---

    [Fact]
    public async Task Finish_Deferred_WhenPayeeTaxComplianceIncomplete_MintsNoInvoice()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;
        await RepointArtistTenantAsync(concert.Id, TenantOf(fixture.SeedState.ArtistManagerNoArtist.Id));

        await fixture.FinishConcertAsync(concert.Id);

        Assert.Null(await InvoiceForBookingAsync(booking.Id));
    }

    // --- Gap-free per-supplier numbering ---

    [Fact]
    public async Task Finish_TwoConcertsSameSupplier_AllocatesGapFreePerSupplierNumbers()
    {
        // Both Past FlatFee and Past DoorSplit are booked to artist 1, so they share a supplier tenant.
        var flatFee = fixture.SeedState.PastFlatFeeBooking;
        var doorSplit = fixture.SeedState.PastDoorSplitBooking;
        await fixture.DeclareDoorRevenueAsync(doorSplit.Concert!.Id, DoorRevenue);

        await fixture.FinishConcertAsync(flatFee.Concert!.Id);
        await fixture.FinishConcertAsync(doorSplit.Concert!.Id);

        var first = await InvoiceForBookingAsync(flatFee.Id);
        var second = await InvoiceForBookingAsync(doorSplit.Id);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Supplier.TenantId, second!.Supplier.TenantId);
        Assert.Equal(1, first.SequenceNumber);
        Assert.Equal(2, second.SequenceNumber);
        Assert.Equal("INV-SEED000001-000001", first.InvoiceNumber);
        Assert.Equal("INV-SEED000001-000002", second.InvoiceNumber);
    }

    // --- Read surface: two-party scoped ---

    [Fact]
    public async Task GetInvoice_IsReadableByPartyVenue_And404ForStranger()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;
        await fixture.FinishConcertAsync(concert.Id);

        var venueUser = fixture.SeedState.Users.Single(u => u.Id == TenantUserOf(concert.VenueTenantId));
        var party = fixture.CreateClient(venueUser);
        var partyResponse = await party.GetAsync($"/api/Concert/{concert.Id}/invoice");
        await partyResponse.ShouldBe(HttpStatusCode.OK);
        Assert.Contains("INV-SEED000001-000001", await partyResponse.Content.ReadAsStringAsync());

        var strangerUser = venueUser.Id == fixture.SeedState.VenueManager1.Id
            ? fixture.SeedState.VenueManager2
            : fixture.SeedState.VenueManager1;
        var stranger = fixture.CreateClient(strangerUser);
        await (await stranger.GetAsync($"/api/Concert/{concert.Id}/invoice")).ShouldBe(HttpStatusCode.NotFound);
    }

    // --- PDF download: two-party scoped, lazy render, self-billing legends ---

    [Fact]
    public async Task GetInvoicePdf_IsDownloadableByBothParties()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;
        await fixture.FinishConcertAsync(concert.Id);

        foreach (var tenantId in new[] { concert.VenueTenantId, concert.ArtistTenantId })
        {
            var party = fixture.CreateClient(UserOfTenant(tenantId));
            var response = await party.GetAsync($"/api/Concert/{concert.Id}/invoice/pdf");

            await response.ShouldBe(HttpStatusCode.OK);
            Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.NotEmpty(bytes);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4)); // the PDF magic number
        }
    }

    [Fact]
    public async Task GetInvoicePdf_Returns404ForStranger()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;
        await fixture.FinishConcertAsync(concert.Id);

        var venueUser = UserOfTenant(concert.VenueTenantId);
        var strangerUser = venueUser.Id == fixture.SeedState.VenueManager1.Id
            ? fixture.SeedState.VenueManager2
            : fixture.SeedState.VenueManager1;

        var response = await fixture.CreateClient(strangerUser).GetAsync($"/api/Concert/{concert.Id}/invoice/pdf");

        // The two-party filter hides the deal document — 404, never a probe-able 403.
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvoicePdf_LazyRendersMissingBlob_UnderInvoicesPrefix()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;
        await fixture.FinishConcertAsync(concert.Id);

        // The blob location is assigned at mint (in the finish transaction) under the invoices/ prefix,
        // before any bytes exist. FakeBlobStorageService reports it absent, so the download exercises the
        // lazy render-on-download path and still returns the PDF.
        var invoice = await InvoiceForBookingAsync(booking.Id);
        Assert.NotNull(invoice);
        Assert.StartsWith("invoices/", invoice!.PdfBlobName);

        var response = await fixture.CreateClient(UserOfTenant(concert.VenueTenantId))
            .GetAsync($"/api/Concert/{concert.Id}/invoice/pdf");
        await response.ShouldBe(HttpStatusCode.OK);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(await response.Content.ReadAsByteArrayAsync(), 0, 4));
    }

    [Fact]
    public async Task GetInvoicePdf_RendersSelfBillingLegends_AndBothPartyVatNumbers()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;
        await SetVatNumberAsync(concert.ArtistTenantId, "GB111111111");   // supplier registered
        await SetVatNumberAsync(concert.VenueTenantId, "GB222222222");    // customer registered
        await fixture.FinishConcertAsync(concert.Id);

        var response = await fixture.CreateClient(UserOfTenant(concert.VenueTenantId))
            .GetAsync($"/api/Concert/{concert.Id}/invoice/pdf");
        await response.ShouldBe(HttpStatusCode.OK);
        var text = Pdf.ExtractText(await response.Content.ReadAsByteArrayAsync());

        Assert.Contains("SELF-BILLING", text);                                 // the HMRC self-billing mark
        Assert.Contains("self-billed invoice", text);                          // raised by the customer on the supplier's behalf
        Assert.Contains("output tax due to HMRC", text);                       // VAT legend (supplier is registered)
        Assert.Contains("GB111111111", text);                                  // supplier VAT number
        Assert.Contains("GB222222222", text);                                  // customer VAT number
    }

    // --- HATEOAS: the invoice link surfaces on the owner read only once the invoice exists ---

    [Fact]
    public async Task ConcertUserRead_ExposesInvoiceLink_OnlyAfterSettlement()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = booking.Concert!;
        var party = fixture.CreateClient(UserOfTenant(concert.VenueTenantId));

        // Before settlement: the party reads its concert, but no invoice exists yet -> no link.
        var before = await (await party.GetAsync($"/api/Concert/user/{concert.Id}")).Content.ReadAsync<ConcertDetailsResponse>();
        Assert.NotNull(before!.Actions);
        Assert.Null(before.Actions!.Invoice);

        await fixture.FinishConcertAsync(concert.Id);

        // After settlement: the minted invoice surfaces its download link.
        var after = await (await party.GetAsync($"/api/Concert/user/{concert.Id}")).Content.ReadAsync<ConcertDetailsResponse>();
        Assert.Equal($"/api/Concert/{concert.Id}/invoice/pdf", after!.Actions!.Invoice!.Href);
    }

    private UserEntity UserOfTenant(Guid tenantId) =>
        fixture.SeedState.Users.Single(u => u.Id == TenantUserOf(tenantId));

    private Guid TenantUserOf(Guid tenantId) =>
        fixture.SeedState.Tenants.Single(t => t.Id == tenantId).CreatedByUserId;
}
