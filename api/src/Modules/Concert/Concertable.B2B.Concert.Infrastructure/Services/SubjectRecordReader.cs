using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class SubjectRecordReader : ISubjectRecordReader
{
    private readonly IConcertReadDbContext context;

    public SubjectRecordReader(IConcertReadDbContext context)
    {
        this.context = context;
    }

    public async Task<SubjectConcertRecordsDto> GetSubjectRecordsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return new SubjectConcertRecordsDto();

        var invoices = await context.Invoices
            .Where(i => tenantIds.Contains(i.VenueTenantId) || tenantIds.Contains(i.ArtistTenantId))
            .ToListAsync(ct);
        var agreements = await context.SelfBillingAgreements
            .Where(s => tenantIds.Contains(s.TenantId))
            .ToListAsync(ct);

        return new SubjectConcertRecordsDto
        {
            Invoices = invoices.Select(i => i.ToSubjectInvoiceDto()).ToList(),
            SelfBillingAgreements = agreements.Select(s => s.ToSubjectSelfBillingAgreementDto()).ToList(),
        };
    }
}
