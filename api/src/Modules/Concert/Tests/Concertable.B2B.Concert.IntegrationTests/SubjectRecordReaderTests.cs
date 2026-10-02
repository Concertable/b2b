using System.Net;
using System.Text.Json;
using Concertable.B2B.Concert.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests;

[Collection("Integration")]
public sealed class SubjectRecordReaderTests : IAsyncLifetime
{
    private const string SupplierIdentifier = "AB123456C";
    private readonly ConcertApiFixture fixture;

    public SubjectRecordReaderTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        this.fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => this.fixture.ResetAsync();
    public Task DisposeAsync() { this.fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task SubjectInvoices_IncludeBothPartiesWithoutCounterpartyTaxIdentifiers()
    {
        var booking = this.fixture.SeedState.PastFlatFeeBooking;
        var concert = this.fixture.SeedState.ConcertFor(booking);
        var supplierUserId = this.UserOfTenant(concert.ArtistTenantId);
        var supplier = this.fixture.CreateClient(this.fixture.SeedState.Users.Single(user => user.Id == supplierUserId));
        var current = await (await supplier.GetAsync("/api/organization")).Content.ReadAsync<JsonElement>();
        var updated = await supplier.PutAsync("/api/organization", new
        {
            legalName = "Individual Supplier",
            contactEmail = current.GetProperty("contactEmail").GetString(),
            expectedVersion = current.GetProperty("version").GetInt64(),
            taxCompliance = new
            {
                vatNumber = (string?)null,
                sellerIdentifier = SupplierIdentifier,
                registeredAddress = new
                {
                    line1 = "1 High Street", line2 = (string?)null, city = "Manchester",
                    postcode = "M1 1AA", country = "United Kingdom",
                },
                bankReference = "GB29NWBK60161331926819",
                holdsMusicLicence = true,
            },
        });
        await updated.ShouldBe(HttpStatusCode.OK);
        var completed = await this.fixture.FinishConcertAsync(concert.Id);
        Assert.True(completed.TryGetValue(out _));
        var stored = await this.fixture.Invoices.SingleAsync(invoice => invoice.BookingId == booking.Id);
        Assert.Contains(SupplierIdentifier, stored.InvoiceNumber);

        foreach (var tenantId in new[] { concert.VenueTenantId, concert.ArtistTenantId })
        {
            var records = await this.ReadAsync(new HashSet<Guid> { tenantId });
            var invoice = Assert.Single(records.Invoices);
            Assert.Equal(DealType.FlatFee, invoice.DealType);
            Assert.Equal(concert.Period.End, invoice.TaxPointUtc);
            Assert.Equal(200m, invoice.Net);
            Assert.Equal(0m, invoice.Vat);
            Assert.Equal(200m, invoice.Gross);
        }
        Assert.Empty((await this.ReadAsync(new HashSet<Guid> { Guid.NewGuid() })).Invoices);
        Assert.Empty((await this.ReadAsync(new HashSet<Guid>())).Invoices);

        var admin = this.fixture.CreateClient(this.fixture.SeedState.Admin);
        var customerId = this.UserOfTenant(concert.VenueTenantId);
        var export = await admin.GetAsync($"/api/subject-export/{customerId}");
        await export.ShouldBe(HttpStatusCode.OK);
        var json = await export.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SupplierIdentifier, json);
        Assert.DoesNotContain(stored.InvoiceNumber, json);
        using var document = JsonDocument.Parse(json);
        var written = Assert.Single(document.RootElement.GetProperty("concertRecords")
            .GetProperty("invoices").EnumerateArray());
        Assert.False(written.TryGetProperty("invoiceNumber", out _));
        Assert.Equal(200m, written.GetProperty("gross").GetDecimal());
    }

    [Fact]
    public async Task SubjectAgreements_IncludeOwnedHistoryAndExcludeOtherTenants()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var expired = this.fixture.SeedNow.AddMonths(-13);
        var current = this.fixture.SeedNow.AddMonths(-1);
        await this.fixture.AddSelfBillingAgreementAsync(owner, expired);
        await this.fixture.AddSelfBillingAgreementAsync(owner, current);
        await this.fixture.AddSelfBillingAgreementAsync(other, this.fixture.SeedNow);

        var records = await this.ReadAsync(new HashSet<Guid> { owner });

        Assert.Empty(records.Invoices);
        Assert.Equal(new[] { expired, current }, records.SelfBillingAgreements
            .Select(agreement => agreement.AcceptedAtUtc).Order().ToArray());
        Assert.All(records.SelfBillingAgreements, agreement =>
        {
            Assert.Equal(agreement.AcceptedAtUtc.AddMonths(12), agreement.ExpiresAtUtc);
            Assert.Equal("2026-07", agreement.PlatformTermsVersion);
        });
        Assert.Empty((await this.ReadAsync(new HashSet<Guid> { Guid.NewGuid() })).SelfBillingAgreements);
        Assert.Empty((await this.ReadAsync(new HashSet<Guid>())).SelfBillingAgreements);
    }

    private Guid UserOfTenant(Guid tenantId) =>
        this.fixture.SeedState.Tenants.Single(tenant => tenant.Id == tenantId).CreatedByUserId;

    private Task<SubjectConcertRecordsDto> ReadAsync(IReadOnlySet<Guid> tenantIds) =>
        this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IConcertModule>().GetSubjectRecordsAsync(tenantIds));
}
