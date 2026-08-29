using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]
public sealed class ConcertSelfBillingGateApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ConcertSelfBillingGateApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task Finish_FixedFee_Defers_WhenSupplierArtistHoldsNoAgreement_AndMintsNoInvoice()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;

        await FinishWithoutGrantingAsync(fixture.SeedState.ConcertFor(booking).Id);

        var persisted = await ConcertAsync(fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(ConcertState.Draft, persisted.State);
        Assert.Null(await InvoiceForBookingAsync(booking.Id));
    }

    [Fact]
    public async Task Finish_VenueHire_Defers_WhenSupplierVenueHoldsNoAgreement()
    {
        var booking = fixture.SeedState.PastVenueHireBooking;

        await FinishWithoutGrantingAsync(fixture.SeedState.ConcertFor(booking).Id);

        var persisted = await ConcertAsync(fixture.SeedState.PastVenueHireApp.Id);
        Assert.Equal(ConcertState.Draft, persisted.State);
        Assert.Null(await InvoiceForBookingAsync(booking.Id));
    }

    [Fact]
    public async Task Finish_SelfHeals_AfterSupplierGrants_AndConsumesNoSequenceNumberAcrossTheDeferral()
    {
        var booking = fixture.SeedState.PastFlatFeeBooking;
        var concert = fixture.SeedState.ConcertFor(booking);

        await FinishWithoutGrantingAsync(concert.Id);
        Assert.Null(await InvoiceForBookingAsync(booking.Id));

        await InsertAgreementAsync(concert.ArtistTenantId);

        // The hourly sweep re-attempts this concert per-id; a direct re-finish is that same call, now in force.
        await FinishWithoutGrantingAsync(concert.Id);

        var persisted = await ConcertAsync(fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);

        var invoice = await InvoiceForBookingAsync(booking.Id);
        Assert.NotNull(invoice);
        Assert.Equal(1, invoice!.SequenceNumber); // the deferral burned no number — this is the supplier's first
        Assert.Equal("INV-SEED000001-000001", invoice.InvoiceNumber);
    }

    private Task<ConcertEntity> ConcertAsync(int applicationId) =>
        fixture.Concerts.FirstAsync(value => value.ApplicationId == applicationId);

    private Task<InvoiceEntity?> InvoiceForBookingAsync(int bookingId) =>
        fixture.Invoices.FirstOrDefaultAsync(invoice => invoice.BookingId == bookingId);

    private async Task FinishWithoutGrantingAsync(int concertId)
    {
        var result = await fixture.CompleteConcertAsync(concertId);
        Assert.True(
            result.IsSuccess,
            result.TryGetError(out var error) ? error.Definition.Message : null);
    }

    // A host (no-HTTP) scope, so the tenant interceptor no-ops and the row keeps the explicit supplier TenantId.
    private async Task InsertAgreementAsync(Guid supplierTenantId)
    {
        await fixture.AddSelfBillingAgreementAsync(supplierTenantId, fixture.SeedNow);
    }
}
