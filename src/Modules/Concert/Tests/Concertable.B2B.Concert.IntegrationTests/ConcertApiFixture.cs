using System.Net;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Executors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Kernel.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Reunion;

namespace Concertable.B2B.Concert.IntegrationTests;

public sealed class ConcertApiFixture : ApiFixture
{
    private IConcertReadDbContext readDbContext = null!;
    private ConcertDbContext dbContext = null!;
    private IScoped<ICompleteExecutor> completeExecutor = null!;
    private ICompletionRunner completionRunner = null!;
    private IConcertService concertService = null!;
    private ISelfBillingAgreementRepository selfBillingAgreementRepository = null!;

    internal IQueryable<ConcertEntity> Concerts => readDbContext.Concerts;
    internal IQueryable<InvoiceEntity> Invoices => dbContext.Invoices.AsNoTracking();
    internal IQueryable<SelfBillingAgreementEntity> SelfBillingAgreements =>
        readDbContext.SelfBillingAgreements;

    internal async Task<Result<SettlementOutcome, FinishConcertError>> FinishConcertAsync(int concertId)
    {
        await EnsureSupplierSelfBillingAgreementAsync(concertId);
        return await completeExecutor.RunAsync(executor => executor.CompleteAsync(concertId));
    }

    internal Task<Result<SettlementOutcome, FinishConcertError>> CompleteConcertAsync(int concertId) =>
        completeExecutor.RunAsync(executor => executor.CompleteAsync(concertId));

    internal Task DeclareDoorRevenueAsync(int concertId, decimal doorRevenue) =>
        concertService.DeclareDoorRevenueAsync(concertId, doorRevenue);

    internal async Task<IDbContextTransaction> HoldConcertForUpdateAsync(int concertId)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            _ = await dbContext.Database.SqlQuery<int>($"""
                    SELECT [Id] AS [Value]
                    FROM [concert].[Concerts] WITH (UPDLOCK, ROWLOCK)
                    WHERE [Id] = {concertId}
                    """)
                .SingleAsync();
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    internal async Task WaitForConcertLockWaitersAsync(int expectedCount)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow <= deadline)
        {
            var count = await dbContext.Database.SqlQuery<int>($"""
                    SELECT COUNT(*) AS [Value]
                    FROM sys.dm_exec_requests AS request
                    CROSS APPLY sys.dm_exec_sql_text(request.sql_handle) AS batch
                    WHERE request.wait_type LIKE N'LCK_M_%'
                      AND CHARINDEX(
                          N'FROM [concert].[Concerts] WITH (UPDLOCK, ROWLOCK)',
                          batch.text) > 0
                    """)
                .SingleAsync();
            if (count >= expectedCount)
                return;

            await Task.Delay(25);
        }

        throw new InvalidOperationException(
            $"Expected {expectedCount} concert transition lock waiter(s).");
    }

    internal Task FailSettlementPersistenceAsync() =>
        dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER [concert].[TR_Concerts_FailSettlementPersistence_ForTest]
            ON [concert].[Concerts]
            AFTER UPDATE
            AS
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM inserted AS current_row
                    INNER JOIN deleted AS prior_row ON prior_row.[Id] = current_row.[Id]
                    WHERE (
                            current_row.[FinancialOperationReferenceId] IS NOT NULL
                            AND prior_row.[FinancialOperationReferenceId] IS NULL
                        ) OR (
                            current_row.[State] <> prior_row.[State]
                            AND prior_row.[SettlementOperationId] IS NOT NULL
                        )
                )
                    THROW 51000, 'Forced settlement persistence failure.', 1;
            END
            """);

    internal Task RestoreSettlementPersistenceAsync() =>
        dbContext.Database.ExecuteSqlRawAsync(
            "DROP TRIGGER IF EXISTS [concert].[TR_Concerts_FailSettlementPersistence_ForTest]");

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
    }

    internal async Task AddSelfBillingAgreementsAsync(
        params SelfBillingAgreementEntity[] agreements)
    {
        dbContext.SelfBillingAgreements.AddRange(agreements);
        await dbContext.SaveChangesAsync();
    }

    internal Task AddSelfBillingAgreementAsync(Guid tenantId, DateTime acceptedAtUtc) =>
        AddSelfBillingAgreementsAsync(CreateAgreement(tenantId, acceptedAtUtc));

    protected override void OnReset(IServiceScope scope)
    {
        readDbContext = scope.ServiceProvider.GetRequiredService<IConcertReadDbContext>();
        dbContext = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
        completeExecutor = scope.ServiceProvider.GetRequiredService<IScoped<ICompleteExecutor>>();
        completionRunner = scope.ServiceProvider.GetRequiredService<ICompletionRunner>();
        concertService = scope.ServiceProvider.GetRequiredService<IConcertService>();
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
