using System.Net;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reunion;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

internal static class ConcertWorkflowExtensions
{
    public static async Task<Result<SettlementOutcome, FinishConcertError>> FinishConcertAsync(
        this ConcertApiFixture fixture,
        int concertId)
    {
        await fixture.EnsureSupplierSelfBillingAgreementAsync(concertId);

        using var scope = fixture.Services.CreateScope();
        var finishExecutor = scope.ServiceProvider.GetRequiredService<IFinishExecutor>();
        return await finishExecutor.FinishAsync(concertId);
    }

    public static async Task DeclareDoorRevenueAsync(this ConcertApiFixture fixture, int concertId, decimal doorRevenue)
    {
        using var scope = fixture.Services.CreateScope();
        var concertService = scope.ServiceProvider.GetRequiredService<IConcertService>();
        await concertService.DeclareDoorRevenueAsync(concertId, doorRevenue);
    }

    public static async Task RunCompletionAsync(this ConcertApiFixture fixture)
    {
        using var scope = fixture.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IConcertCompletionRunner>();
        await runner.RunAsync();
    }

    private static async Task EnsureSupplierSelfBillingAgreementAsync(this ConcertApiFixture fixture, int concertId)
    {
        using var scope = fixture.Services.CreateScope();
        var concert = await scope.ServiceProvider.GetRequiredService<IConcertRepository>().GetByIdWithBookingAsync(concertId);
        if (concert is null)
            return;

        var supplierTenantId = scope.ServiceProvider.GetRequiredService<ISettlementPayeeResolver>().ResolveTenantId(concert);
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var context = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
        if (await context.SelfBillingAgreements.AnyAsync(a => a.TenantId == supplierTenantId && a.ExpiresAtUtc > now))
            return;

        context.SelfBillingAgreements.Add(SelfBillingAgreementEntity.Create(
            supplierTenantId,
            new InvoiceParty(supplierTenantId, "Sally Supplier Ltd", "GB123456789", "1 Road", null, "Town", "AB1 2CD", "United Kingdom"),
            new ESignature(Guid.NewGuid(), now, IPAddress.Loopback, "supplier-agent", "Sally Supplier", null),
            "This self-billing agreement authorises self-billed invoices.",
            "2026-07",
            now,
            now));
        await context.SaveChangesAsync();
    }
}
