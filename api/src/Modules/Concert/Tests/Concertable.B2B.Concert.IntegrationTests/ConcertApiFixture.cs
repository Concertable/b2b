using System.Net;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Testing.Integration;
using Concertable.Kernel.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reunion;

namespace Concertable.B2B.Concert.IntegrationTests;

public sealed class ConcertApiFixture : ApiFixture
{
    private IConcertReadDbContext readDbContext = null!;
    private ConcertPrivilegedDbContext dbContext = null!;
    private IScoped<IConcertWorkflow> workflow = null!;
    private ICompletionRunner completionRunner = null!;
    private ISelfBillingAgreementRepository selfBillingAgreementRepository = null!;

    internal IQueryable<ConcertEntity> Concerts => readDbContext.Concerts;

    internal async Task<HttpResponseMessage> GetConcertByApplicationAsync(HttpClient client, int applicationId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        do
        {
            var concertId = await dbContext.Concerts
                .AsNoTracking()
                .Where(concert => concert.ApplicationId == applicationId)
                .Select(concert => (int?)concert.Id)
                .SingleOrDefaultAsync();
            if (concertId is not null)
                return await client.GetAsync($"/api/concert/{concertId}/operations");

            await Task.Delay(100);
        }
        while (DateTimeOffset.UtcNow <= deadline);

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
    internal IQueryable<InvoiceEntity> Invoices => dbContext.Invoices.AsNoTracking();
    internal IQueryable<SelfBillingAgreementEntity> SelfBillingAgreements =>
        readDbContext.SelfBillingAgreements;

    internal async Task<Result<SettlementOutcome, FinishConcertError>> FinishConcertAsync(int concertId)
    {
        await EnsureSupplierSelfBillingAgreementAsync(concertId);
        return await workflow.RunAsync(workflow => workflow.CompleteAsync(concertId));
    }

    internal Task<Result<SettlementOutcome, FinishConcertError>> CompleteConcertAsync(int concertId) =>
        workflow.RunAsync(workflow => workflow.CompleteAsync(concertId));

    internal async Task DeclareDoorRevenueAsync(
        int concertId,
        decimal doorRevenue)
    {
        var entity = await dbContext.Concerts
            .SingleOrDefaultAsync(concert => concert.Id == concertId);

        if (entity is not DoorRevenueConcert concert)
            throw new InvalidOperationException($"Concert {concertId} was not a door-revenue concert.");

        var result = concert.DeclareDoorRevenue(doorRevenue);
        if (result.TryGetError(out var error))
            throw new InvalidOperationException(error.ToString());

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Commits <paramref name="competingChange"/> between the next concert transition's read and its
    /// update, so that transition loses the race and has to rerun against the winner's state.
    /// </summary>
    // A CHECK constraint rather than a trigger: EF reads the row version back with an OUTPUT clause,
    // and SQL Server rejects OUTPUT against a table that has an enabled trigger. Stated over the new
    // row alone, it still admits the settlement reservation and rejects only what follows it.
    internal Task FailSettlementPersistenceAsync()
    {
        var settlementOperationId = dbContext.Database.DelimitIdentifier("SettlementOperationId");
        var state = dbContext.Database.DelimitIdentifier("State");
        return dbContext.Database.AddUnvalidatedCheckConstraintAsync(
            "concert",
            "Concerts",
            "CK_Concerts_FailSettlementPersistence_ForTest",
            $"{settlementOperationId} IS NULL OR {state} = {(int)ConcertState.AwaitingSettlement}");
    }

    internal Task RestoreSettlementPersistenceAsync() =>
        dbContext.Database.DropCheckConstraintIfExistsAsync(
            "concert",
            "Concerts",
            "CK_Concerts_FailSettlementPersistence_ForTest");

    internal Task RunCompletionAsync() => completionRunner.RunAsync();

    internal Task<bool> HasCurrentSelfBillingAgreementAsync(Guid tenantId, DateTime now) =>
        selfBillingAgreementRepository.ExistsCurrentByTenantIdAsync(tenantId, now);

    internal async Task RepointConcertTenantsAsync(
        int concertId,
        Guid? artistTenantId = null,
        Guid? venueTenantId = null)
    {
        if (artistTenantId is { } artist)
            await dbContext.Concerts.Where(concert => concert.Id == concertId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    concert => concert.ArtistTenantId,
                    artist));
        if (venueTenantId is { } venue)
            await dbContext.Concerts.Where(concert => concert.Id == concertId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    concert => concert.VenueTenantId,
                    venue));

        dbContext.ChangeTracker.Clear();
    }

    internal async Task AddSelfBillingAgreementsAsync(
        params SelfBillingAgreementEntity[] agreements)
    {
        dbContext.SelfBillingAgreements.AddRange(agreements);
        await dbContext.SaveChangesAsync();
    }

    internal Task AddSelfBillingAgreementAsync(Guid tenantId, DateTime acceptedAtUtc) =>
        AddSelfBillingAgreementsAsync(CreateAgreement(tenantId, acceptedAtUtc));

    protected override void OnConfigureServices(IServiceCollection services)
    {
    }

    protected override void OnReset(IServiceScope scope)
    {
        readDbContext = scope.ServiceProvider.GetRequiredService<IConcertReadDbContext>();
        dbContext = scope.ServiceProvider.GetRequiredService<ConcertPrivilegedDbContext>();
        workflow = scope.ServiceProvider.GetRequiredService<IScoped<IConcertWorkflow>>();
        completionRunner = scope.ServiceProvider.GetRequiredService<ICompletionRunner>();
        selfBillingAgreementRepository = scope.ServiceProvider
            .GetRequiredService<ISelfBillingAgreementRepository>();
    }

    internal async Task EnsureSupplierSelfBillingAgreementAsync(int concertId)
    {
        var concert = await dbContext.Concerts.SingleOrDefaultAsync(value => value.Id == concertId);
        if (concert is null)
            return;

        var supplierTenantId = concert.SettlementPayeeTenantId;
        var now = SeedNow;
        if (await dbContext.SelfBillingAgreements.AnyAsync(
                agreement => agreement.TenantId == supplierTenantId && agreement.ExpiresAtUtc > now))
            return;

        await AddSelfBillingAgreementAsync(supplierTenantId, now);
    }

    private static SelfBillingAgreementEntity CreateAgreement(Guid tenantId, DateTime acceptedAtUtc) =>
        SelfBillingAgreementEntity.Create(
            tenantId,
            new InvoiceParty(
                tenantId,
                "Sally Supplier Ltd",
                "GB123456789",
                "1 Road",
                null,
                "Town",
                "AB1 2CD",
                "United Kingdom"),
            new ESignature(
                Guid.NewGuid(),
                acceptedAtUtc,
                IPAddress.Loopback,
                "supplier-agent",
                "Sally Supplier",
                null),
            "This self-billing agreement authorises self-billed invoices.",
            "2026-07",
            acceptedAtUtc,
            acceptedAtUtc);
}
