using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class SequenceRepository : ISequenceRepository
{
    private readonly ConcertDbContext context;

    public SequenceRepository(ConcertDbContext context)
    {
        this.context = context;
    }

    public async Task<long> AllocateNextAsync(Guid ownerId, CancellationToken ct = default)
    {
        var sequence = await context.InvoiceSequences.FirstOrDefaultAsync(s => s.TenantId == ownerId, ct);
        if (sequence is null)
        {
            sequence = InvoiceSequenceEntity.Start(ownerId);
            await context.InvoiceSequences.AddAsync(sequence, ct);
        }

        return sequence.Allocate();
    }
}
