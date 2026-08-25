using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.IntegrationTests;

public sealed class ApplicationApiFixture : ApiFixture
{
    private IApplicationReadDbContext readDbContext = null!;
    private ApplicationDbContext dbContext = null!;

    internal IQueryable<ApplicationEntity> Applications => readDbContext.Applications;
    internal IQueryable<ConcertAvailabilityEntity> ConcertAvailabilities => readDbContext.ConcertAvailabilities;
    internal IQueryable<VerifyPaymentEntity> PaymentVerifications => readDbContext.VerifyPayments;

    internal async Task<IDbContextTransaction> HoldApplicationForUpdateAsync(int applicationId)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            _ = await dbContext.Database.SqlQuery<int>($"""
                    SELECT [Id] AS [Value]
                    FROM [application].[Applications] WITH (UPDLOCK, ROWLOCK)
                    WHERE [Id] = {applicationId}
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

    internal async Task WaitForApplicationLockWaitersAsync(int expectedCount)
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
                          N'FROM [application].[Applications] WITH (UPDLOCK, ROWLOCK)',
                          batch.text) > 0
                    """)
                .SingleAsync();
            if (count >= expectedCount)
                return;

            await Task.Delay(25);
        }

        throw new InvalidOperationException(
            $"Expected {expectedCount} application transition lock waiter(s).");
    }

    protected override void OnReset(IServiceScope scope)
    {
        readDbContext = scope.ServiceProvider.GetRequiredService<IApplicationReadDbContext>();
        dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }
}
