using System.Net;
using System.Runtime.ExceptionServices;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Testing.Integration;
using Concertable.Kernel.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Reunion;

namespace Concertable.B2B.Concert.IntegrationTests;

public sealed class ConcertApiFixture : ApiFixture
{
    private const long ReceiptInsertBarrierKey = 638_457_219;

    private IConcertReadDbContext readDbContext = null!;
    private ConcertPrivilegedDbContext dbContext = null!;
    private IScoped<IConcertWorkflow> workflow = null!;
    private ICompletionRunner completionRunner = null!;
    private ISelfBillingAgreementRepository selfBillingAgreementRepository = null!;
    private TimeProvider timeProvider = null!;
    private string connectionString = null!;

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

    internal async Task<(Guid GrantId, long AccessVersion, DateTime Now)> AddExpiredSummaryShareAsync(
        int concertId,
        Guid issuerTenantId,
        Guid issuerUserId,
        Guid recipientTenantId)
    {
        dbContext.ChangeTracker.Clear();
        var concert = await dbContext.Concerts
            .Include(value => value.AccessGrants)
            .SingleAsync(value => value.Id == concertId);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var share = concert.ShareSummary(
            issuerTenantId,
            issuerUserId,
            recipientTenantId,
            null,
            now.AddDays(-2),
            now.AddDays(-1));
        if (!share.TryGetValue(out var grant))
            throw new InvalidOperationException("Could not seed an expired summary share.");

        dbContext.Add(grant);
        await dbContext.SaveChangesAsync();
        return (grant.Id, concert.AccessVersion, now);
    }

    internal async Task<T> RunWithReceiptInsertBarrierAsync<T>(
        Func<CancellationToken, Task<T>> action) =>
        await RunWithReceiptInsertBarrierCoreAsync(action, null);

    internal async Task<T> RunWithReceiptInsertBarrierAsync<T>(
        Func<CancellationToken, Task<T>> action,
        Exception injectedFailure) =>
        await RunWithReceiptInsertBarrierCoreAsync(action, injectedFailure);

    private async Task<T> RunWithReceiptInsertBarrierCoreAsync<T>(
        Func<CancellationToken, Task<T>> action,
        Exception? injectedFailure)
    {
        using var cancellation = new CancellationTokenSource();
        await using var control = new NpgsqlConnection(connectionString);
        Task<T>? result = null;
        ExceptionDispatchInfo? failure = null;
        T? outcome = default;
        var completed = false;
        var lockHeld = false;

        try
        {
            await control.OpenAsync();
            await ExecuteAsync(control, """
                CREATE OR REPLACE FUNCTION concert.block_receipt_insert_for_test()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock_shared(638457219);
                    RETURN NEW;
                END;
                $$;
                DROP TRIGGER IF EXISTS block_receipt_insert_for_test
                    ON concert."ConcertCommandReceipts";
                CREATE TRIGGER block_receipt_insert_for_test
                    BEFORE INSERT ON concert."ConcertCommandReceipts"
                    FOR EACH ROW EXECUTE FUNCTION concert.block_receipt_insert_for_test();
                """);
            await ExecuteAsync(control, $"SELECT pg_advisory_lock({ReceiptInsertBarrierKey})");
            lockHeld = true;
            result = action(cancellation.Token);
            await WaitForReceiptInsertWaitersAsync(connectionString);
            if (injectedFailure is not null)
                throw injectedFailure;
            await ExecuteAsync(control, $"SELECT pg_advisory_unlock({ReceiptInsertBarrierKey})");
            lockHeld = false;
            outcome = await result;
            completed = true;
        }
        catch (Exception exception)
        {
            failure = ExceptionDispatchInfo.Capture(exception);
            try
            {
                await cancellation.CancelAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception cancellationException)
            {
                failure ??= ExceptionDispatchInfo.Capture(cancellationException);
            }
        }

        if (lockHeld)
        {
            try
            {
                await ExecuteAsync(control, $"SELECT pg_advisory_unlock({ReceiptInsertBarrierKey})");
                lockHeld = false;
            }
            catch (Exception exception)
            {
                failure ??= ExceptionDispatchInfo.Capture(exception);
                try
                {
                    await control.CloseAsync();
                }
                catch (Exception closeException)
                {
                    failure ??= ExceptionDispatchInfo.Capture(closeException);
                }
            }
        }

        if (result is not null && !completed)
        {
            try
            {
                outcome = await result.WaitAsync(TimeSpan.FromSeconds(10));
                completed = true;
            }
            catch (Exception exception)
            {
                failure ??= ExceptionDispatchInfo.Capture(exception);
            }
        }

        try
        {
            using var cleanupCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var cleanup = new NpgsqlConnection(connectionString);
            await cleanup.OpenAsync(cleanupCancellation.Token);
            await ExecuteAsync(cleanup, """
                SET lock_timeout = '5s';
                DROP TRIGGER IF EXISTS block_receipt_insert_for_test
                    ON concert."ConcertCommandReceipts";
                DROP FUNCTION IF EXISTS concert.block_receipt_insert_for_test();
                """, cleanupCancellation.Token);
        }
        catch (Exception exception)
        {
            failure ??= ExceptionDispatchInfo.Capture(exception);
        }

        failure?.Throw();
        if (!completed)
            throw new InvalidOperationException("The receipt insert barrier action did not complete.");
        return outcome!;
    }

    internal async Task<bool> HasReceiptInsertBarrierAsync()
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM pg_trigger
                WHERE tgname = 'block_receipt_insert_for_test'
                  AND NOT tgisinternal)
            OR EXISTS (
                SELECT 1
                FROM pg_proc AS procedure
                INNER JOIN pg_namespace AS schema ON schema.oid = procedure.pronamespace
                WHERE schema.nspname = 'concert'
                  AND procedure.proname = 'block_receipt_insert_for_test')
            """;
        return (bool)(await command.ExecuteScalarAsync() ?? true);
    }

    private static async Task WaitForReceiptInsertWaitersAsync(string connectionString)
    {
        await using var observer = new NpgsqlConnection(connectionString);
        await observer.OpenAsync();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        do
        {
            await using var command = observer.CreateCommand();
            command.CommandText = """
                SELECT count(*)
                FROM pg_stat_activity
                WHERE datname = current_database()
                  AND pid <> pg_backend_pid()
                  AND wait_event_type = 'Lock'
                  AND query LIKE '%ConcertCommandReceipts%'
                """;
            if ((long)(await command.ExecuteScalarAsync() ?? 0L) >= 2)
                return;
            await Task.Delay(20);
        }
        while (DateTimeOffset.UtcNow < deadline);

        throw new TimeoutException("Two receipt inserts did not reach the database barrier.");
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

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
        timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        connectionString = scope.ServiceProvider
            .GetRequiredService<IConfiguration>()
            .GetConnectionString(B2BDb.Name)
            ?? throw new InvalidOperationException($"Connection string '{B2BDb.Name}' is required.");
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
