using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class InvoiceSequenceRepository : IInvoiceSequenceRepository
{
    private readonly ConcertDbContext context;

    public InvoiceSequenceRepository(ConcertDbContext context)
    {
        this.context = context;
    }

    public async Task<long> AllocateNextAsync(Guid tenantId, CancellationToken ct = default)
    {
        var sequence = await context.InvoiceSequences.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        if (sequence is null)
        {
            sequence = InvoiceSequenceEntity.Start(tenantId);
            await context.InvoiceSequences.AddAsync(sequence, ct);
        }

        return sequence.Allocate();
    }
}
