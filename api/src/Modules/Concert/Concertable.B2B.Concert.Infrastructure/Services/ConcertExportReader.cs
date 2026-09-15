using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertExportReader : IConcertExportReader
{
    private readonly IConcertReadDbContext context;

    public ConcertExportReader(IConcertReadDbContext context)
    {
        this.context = context;
    }

    public async Task<ConcertExport> GetConcertExportAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return new ConcertExport();

        var invoices = await context.Invoices
            .Where(i => tenantIds.Contains(i.VenueTenantId) || tenantIds.Contains(i.ArtistTenantId))
            .ToListAsync(ct);
        var agreements = await context.SelfBillingAgreements
            .Where(s => tenantIds.Contains(s.TenantId))
            .ToListAsync(ct);

        return new ConcertExport
        {
            Invoices = invoices.Select(i => i.ToInvoiceExport()).ToList(),
            SelfBillingAgreements = agreements.Select(s => s.ToSelfBillingAgreementExport()).ToList(),
        };
    }
}
