using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertRecordsExporter : IConcertRecordsExporter
{
    private readonly IConcertReadDbContext context;

    public ConcertRecordsExporter(IConcertReadDbContext context)
    {
        this.context = context;
    }

    public async Task<ConcertRecordsExport> ExportAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return new ConcertRecordsExport();

        var invoices = await context.Invoices
            .Where(i => tenantIds.Contains(i.VenueTenantId) || tenantIds.Contains(i.ArtistTenantId))
            .ToListAsync(ct);
        var agreements = await context.SelfBillingAgreements
            .Where(s => tenantIds.Contains(s.TenantId))
            .ToListAsync(ct);

        return new ConcertRecordsExport
        {
            Invoices = invoices.Select(i => i.ToInvoiceExport()).ToList(),
            SelfBillingAgreements = agreements.Select(s => s.ToSelfBillingAgreementExport()).ToList(),
        };
    }
}
