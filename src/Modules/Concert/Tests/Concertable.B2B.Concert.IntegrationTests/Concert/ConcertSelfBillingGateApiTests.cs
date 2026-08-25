using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

/// <summary>
/// The fail-closed self-billing gate (<c>FinishExecutor</c>, after the tax gate): the settlement's supplier — the
/// seller in whose name the self-billed invoice is raised — must hold a current self-billing agreement, or the
/// concert is not transitioned and no invoice is minted (it self-heals on the next sweep once the supplier grants).
/// The supplier is direction-dependent: the artist for revenue-share/fixed-fee, the venue for VenueHire. These drive
/// finish directly (not the auto-granting <c>FinishConcertAsync</c> helper) so a tax-complete supplier reaches this
/// gate with no agreement. Because a deferral returns before the invoice is minted, no per-supplier number is burned.
/// </summary>
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
        Assert.Equal(State.Draft, persisted.State);
        Assert.Null(await InvoiceForBookingAsync(booking.Id));
    }

    [Fact]
    public async Task Finish_VenueHire_Defers_WhenSupplierVenueHoldsNoAgreement()
    {
        var booking = fixture.SeedState.PastVenueHireBooking;

        await FinishWithoutGrantingAsync(fixture.SeedState.ConcertFor(booking).Id);

        var persisted = await ConcertAsync(fixture.SeedState.PastVenueHireApp.Id);
        Assert.Equal(State.Draft, persisted.State);
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
        Assert.Equal(State.Complete, persisted.State);

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
