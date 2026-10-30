using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Contracts.Enums;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Booking.IntegrationTests;

[Collection("Integration")]
public sealed class SubjectContractReaderTests : IAsyncLifetime
{
    private readonly BookingApiFixture fixture;

    public SubjectContractReaderTests(BookingApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        this.fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => this.fixture.ResetAsync();
    public Task DisposeAsync() { this.fixture.DetachOutput(); return Task.CompletedTask; }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SubjectContracts_IncludeEconomicPartiesAndExcludeUnrelatedBookings(
        bool includeVenue, bool includeArtist)
    {
        var venueTenantId = Guid.NewGuid();
        var artistTenantId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2035, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await this.AddContractAsync(900001, venueTenantId, artistTenantId, createdAtUtc);
        await this.AddContractAsync(900002, Guid.NewGuid(), Guid.NewGuid(), createdAtUtc.AddDays(1));
        var tenantIds = new HashSet<Guid>();
        if (includeVenue)
            tenantIds.Add(venueTenantId);
        if (includeArtist)
            tenantIds.Add(artistTenantId);

        var actual = await this.ReadAsync(tenantIds);

        var contract = Assert.Single(actual);
        Assert.Equal(DealType.FlatFee, contract.DealType);
        Assert.Equal(createdAtUtc, contract.CreatedAtUtc);
        Assert.Empty(await this.ReadAsync(new HashSet<Guid> { Guid.NewGuid() }));
        Assert.Empty(await this.ReadAsync(new HashSet<Guid>()));
    }

    private Task AddContractAsync(int applicationId, Guid venueTenantId, Guid artistTenantId,
        DateTime createdAtUtc) => this.fixture.Services.RunScopedAsync(async sp =>
    {
        var terms = new FlatFeeTerms(100m);
        var signature = new ContractSignature(Guid.NewGuid(), createdAtUtc, IPAddress.Loopback,
            "tests", "Signatory", null);
        var snapshot = new ApplicationAcceptanceSnapshot(Guid.NewGuid(),
            new ApplicationSnapshot(applicationId, new ArtistSnapshot(44, artistTenantId, "Artist"),
                new OpportunitySnapshot(applicationId, new VenueSnapshot(45, venueTenantId, "Venue"),
                    createdAtUtc.AddDays(1), createdAtUtc.AddDays(1).AddHours(3), [Genre.Rock])),
            new ContractSnapshot(PaymentMethod.Transfer, "Terms", "1", "2026-09",
                PaymentOperationReferences.EscrowHold(applicationId), signature, signature, terms));
        var context = sp.GetRequiredService<BookingPrivilegedDbContext>();
        var booking = BookingEntity.Create(snapshot);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        booking.MintContract(FlatFeeContract.Create(booking.Id, snapshot, terms, createdAtUtc));
        await context.SaveChangesAsync();
        return booking.Id;
    });

    private Task<IReadOnlyList<SubjectContractDto>> ReadAsync(IReadOnlySet<Guid> tenantIds) =>
        this.fixture.Services.RunScopedAsync(sp =>
            sp.GetRequiredService<IBookingModule>().GetSubjectContractsAsync(tenantIds));
}
