using System.Net;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

/// <summary>
/// The system self-billing gate (<c>ISelfBillingAgreementGate</c>) reads a supplier's agreements filter-free
/// by explicit tenant id — the stance <c>FinishExecutor</c> and the tenant-less hourly sweep use. A supplier is
/// in force only while an unexpired agreement exists; the frozen supplier identity + e-signature round-trip
/// through the real DB. (Owner-scoped self-service reads + PDF download arrive with endpoints in Phase 2.)
/// </summary>
[Collection("Integration")]
public sealed class SelfBillingAgreementGateApiTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly ConcertApiFixture fixture;

    public SelfBillingAgreementGateApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private static SelfBillingAgreementEntity Agreement(Guid tenantId, DateTime acceptedAtUtc) =>
        SelfBillingAgreementEntity.Create(
            tenantId,
            new InvoiceParty(tenantId, "Sally Supplier Ltd", "GB123456789", "1 Road", null, "Town", "AB1 2CD", "United Kingdom"),
            new ESignature(Guid.NewGuid(), acceptedAtUtc, IPAddress.Loopback, "supplier-agent", "Sally Supplier", null),
            "This self-billing agreement authorises self-billed invoices.",
            "2026-07",
            acceptedAtUtc,
            acceptedAtUtc);

    [Fact]
    public async Task Gate_IsInForceOnlyWhileACurrentAgreementExists_AndFrozenIdentityRoundTrips()
    {
        var inForce = Guid.NewGuid();
        var lapsed = Guid.NewGuid();
        var never = Guid.NewGuid();

        // A background (no-HTTP) scope is host, so the tenant interceptor no-ops and rows keep their explicit TenantId.
        using (var seed = fixture.Services.CreateScope())
        {
            var context = seed.ServiceProvider.GetRequiredService<TenantConcertDbContext>();
            context.SelfBillingAgreements.AddRange(
                Agreement(inForce, Now.AddMonths(-13)),
                Agreement(inForce, Now.AddMonths(-1)),
                Agreement(lapsed, Now.AddMonths(-13)));
            await context.SaveChangesAsync();
        }

        using var scope = fixture.Services.CreateScope();
        var gate = scope.ServiceProvider.GetRequiredService<ISelfBillingAgreementGate>();

        Assert.True(await gate.HasCurrentAsync(inForce, Now));
        Assert.False(await gate.HasCurrentAsync(lapsed, Now));
        Assert.False(await gate.HasCurrentAsync(never, Now));

        var current = await fixture.ConcertReads.Set<SelfBillingAgreementEntity>()
            .Where(a => a.TenantId == inForce && a.ExpiresAtUtc > Now)
            .SingleAsync();
        Assert.Equal(current.AcceptedAtUtc.AddMonths(12), current.ExpiresAtUtc);
        Assert.Equal("Sally Supplier Ltd", current.Supplier.LegalName);
        Assert.Equal("GB123456789", current.Supplier.VatNumber);
        Assert.Equal("Sally Supplier", current.SupplierESignature.SignatoryName);
        Assert.StartsWith("self-billing-agreements/", current.PdfBlobName);
    }
}
