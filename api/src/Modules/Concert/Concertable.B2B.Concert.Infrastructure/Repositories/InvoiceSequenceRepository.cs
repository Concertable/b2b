using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class InvoiceSequenceRepository : IInvoiceSequenceRepository
{
    private readonly ConcertPrivilegedDbContext context;

    public InvoiceSequenceRepository(ConcertPrivilegedDbContext context)
    {
        this.context = context;
    }

    public async Task<InvoiceSequenceEntity?> GetByTenantIdForUpdateAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM concert."InvoiceSequences"
            WHERE "TenantId" = {tenantId}
            FOR UPDATE
            """, ct);
        return await context.InvoiceSequences.SingleOrDefaultAsync(
            sequence => sequence.TenantId == tenantId,
            ct);
    }

    public async Task InsertAsync(InvoiceSequenceEntity sequence, CancellationToken ct = default) =>
        await context.InvoiceSequences.AddAsync(sequence, ct);
}
