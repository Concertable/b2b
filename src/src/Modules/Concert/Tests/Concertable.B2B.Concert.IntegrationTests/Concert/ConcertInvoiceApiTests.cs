using System.Net;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Deal.Contracts;
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

    private Guid TenantUserOf(Guid tenantId) =>
        fixture.SeedState.Tenants.Single(t => t.Id == tenantId).CreatedByUserId;
}
