using System.Net;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Kernel.ValueObjects;
using Concertable.Testing.Integration;
using Concertable.Kernel.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reunion;

namespace Concertable.B2B.Concert.IntegrationTests;

public sealed class ConcertApiFixture : ApiFixture
{
    private IConcertReadDbContext readDbContext = null!;
    private ConcertDbContext dbContext = null!;
    private IScoped<IConcertWorkflow> workflow = null!;
    private ICompletionRunner completionRunner = null!;
    private IConcertService concertService = null!;
    private ISelfBillingAgreementRepository selfBillingAgreementRepository = null!;

    internal ConcurrencyConflictInterceptor Conflicts { get; } = new();

    internal IQueryable<ConcertEntity> Concerts => readDbContext.Concerts;

    /// <summary>
    /// The concert is created by an event dispatched after the request that confirmed the booking has
    /// returned, so reading it straight after the webhook races the dispatcher.
    /// </summary>
    internal async Task<HttpResponseMessage> GetConcertByApplicationAsync(HttpClient client, int applicationId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        HttpResponseMessage response;
        do
        {
            response = await client.GetAsync($"/api/concert/application/{applicationId}");
            if (response.StatusCode != HttpStatusCode.NotFound)
                return response;

            await Task.Delay(100);
        }
        while (DateTimeOffset.UtcNow <= deadline);

        return response;
    }
    internal IQueryable<InvoiceEntity> Invoices => readDbContext.Invoices;
    internal IQueryable<SelfBillingAgreementEntity> SelfBillingAgreements =>
        readDbContext.SelfBillingAgreements;

    internal async Task<Result<SettlementOutcome, FinishConcertError>> FinishConcertAsync(int concertId)
    {
        await EnsureSupplierSelfBillingAgreementAsync(concertId);
        return await CompleteConcertAsync(concertId);
    }

    internal Task<Result<SettlementOutcome, FinishConcertError>> CompleteConcertAsync(int concertId) =>
        AsSettlementPayeeAsync(
            concertId,
            () => workflow.RunAsync(workflow => workflow.CompleteAsync(concertId)));

    internal Task DeclareDoorRevenueAsync(int concertId, decimal doorRevenue) =>
        AsVenueAsync(concertId, () => concertService.DeclareDoorRevenueAsync(concertId, doorRevenue));

    /// <summary>
    /// The sweep and the HTTP terminal both reach the workflow with a tenant established; these helpers
    /// stand in for it, picking the same tenant that caller would have carried.
    /// </summary>
    private async Task<T> AsSettlementPayeeAsync<T>(int concertId, Func<Task<T>> action)
    {
        var concert = await readDbContext.Concerts
            .SingleOrDefaultAsync(value => value.Id == concertId);
        if (concert is null)
            return await action();

        using var acting = TenantScope.As(concert.SettlementPayeeTenantId);
        return await action();
    }

    private async Task AsVenueAsync(int concertId, Func<Task> action)
    {
        var venueTenantId = await readDbContext.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => (Guid?)concert.VenueTenantId)
            .SingleOrDefaultAsync();
        if (venueTenantId is null)
        {
            await action();
            return;
        }

        using var acting = TenantScope.As(venueTenantId.Value);
        await action();
    }

    /// <summary>
    /// Commits <paramref name="competingChange"/> between the next concert transition's read and its
    /// update, so that transition loses the race and has to rerun against the winner's state.
    /// </summary>
    internal void ArmConcertConflict(Func<Task> competingChange) =>
        Conflicts.ArmOnce<ConcertEntity>(competingChange);

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
        var owner = await readDbContext.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => (Guid?)concert.VenueTenantId)
            .SingleOrDefaultAsync();
        if (owner is null)
            return;

        using var acting = TenantScope.As(owner.Value);
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
    }

    internal async Task SetConcertPeriodAsync(int concertId, DateRange period)
    {
        var venueTenantId = await readDbContext.Concerts
            .Where(concert => concert.Id == concertId)
            .Select(concert => (Guid?)concert.VenueTenantId)
            .SingleOrDefaultAsync();
        if (venueTenantId is null)
            return;

        await using var scope = Services.CreateAsyncScope();
        using var acting = TenantScope.As(venueTenantId.Value);
        var context = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
        var concert = await context.Concerts.SingleAsync(entity => entity.Id == concertId);
        context.Entry(concert).ComplexProperty(entity => entity.Period).CurrentValue = period;
        await context.SaveChangesAsync();
    }

    internal async Task AddSelfBillingAgreementsAsync(
        params SelfBillingAgreementEntity[] agreements)
    {
        foreach (var owned in agreements.GroupBy(agreement => agreement.TenantId))
        {
            using var acting = TenantScope.As(owned.Key);
            dbContext.SelfBillingAgreements.AddRange(owned);
            await dbContext.SaveChangesAsync();
        }
    }

    private ITenantScope TenantScope => Services.GetRequiredService<ITenantScope>();

    internal Task AddSelfBillingAgreementAsync(Guid tenantId, DateTime acceptedAtUtc) =>
        AddSelfBillingAgreementsAsync(CreateAgreement(tenantId, acceptedAtUtc));

    protected override void OnConfigureServices(IServiceCollection services)
    {
        services.AddResettables(Conflicts);
        services.ConfigureDbContext<ConcertDbContext>(
            (_, options) => options.AddInterceptors(Conflicts));
    }

    protected override void OnReset(IServiceScope scope)
    {
        readDbContext = scope.ServiceProvider.GetRequiredService<IConcertReadDbContext>();
        dbContext = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
        workflow = scope.ServiceProvider.GetRequiredService<IScoped<IConcertWorkflow>>();
        completionRunner = scope.ServiceProvider.GetRequiredService<ICompletionRunner>();
        concertService = scope.ServiceProvider.GetRequiredService<IConcertService>();
        selfBillingAgreementRepository = scope.ServiceProvider
            .GetRequiredService<ISelfBillingAgreementRepository>();
    }

    internal async Task EnsureSupplierSelfBillingAgreementAsync(int concertId)
    {
        var concert = await readDbContext.Concerts.SingleOrDefaultAsync(value => value.Id == concertId);
        if (concert is null)
            return;

        var supplierTenantId = concert.SettlementPayeeTenantId;
        var now = SeedNow;
        if (await readDbContext.SelfBillingAgreements.AnyAsync(
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
