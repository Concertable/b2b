using System.Net;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]
public sealed class SelfBillingAgreementRepositoryApiTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly ConcertApiFixture fixture;

    public SelfBillingAgreementRepositoryApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
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
    public async Task ExistsCurrentByTenantIdAsync_IsTrueOnlyForAnInForceAgreement()
    {
        var inForce = Guid.NewGuid();
        var lapsed = Guid.NewGuid();
        var never = Guid.NewGuid();

        await fixture.AddSelfBillingAgreementsAsync(
            Agreement(inForce, Now.AddMonths(-13)),
            Agreement(inForce, Now.AddMonths(-1)),
            Agreement(lapsed, Now.AddMonths(-13)));

        Assert.True(await fixture.HasCurrentSelfBillingAgreementAsync(inForce, Now));
        Assert.False(await fixture.HasCurrentSelfBillingAgreementAsync(lapsed, Now));
        Assert.False(await fixture.HasCurrentSelfBillingAgreementAsync(never, Now));

        var current = await fixture.SelfBillingAgreements
            .Where(a => a.TenantId == inForce && a.ExpiresAtUtc > Now)
            .SingleAsync();
        Assert.Equal(current.AcceptedAtUtc.AddMonths(12), current.ExpiresAtUtc);
        Assert.Equal("Sally Supplier Ltd", current.Supplier.LegalName);
        Assert.Equal("GB123456789", current.Supplier.VatNumber);
        Assert.Equal("Sally Supplier", current.SupplierESignature.SignatoryName);
        Assert.StartsWith("self-billing-agreements/", current.PdfBlobName);
    }
}
