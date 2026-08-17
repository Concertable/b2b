using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class SequenceRepository<TSequence> : ISequenceRepository<TSequence>
    where TSequence : class, ISequence<TSequence>
{
    private readonly ConcertDbContext context;

    public SequenceRepository(ConcertDbContext context)
    {
        this.context = context;
    }

    public async Task<long> AllocateNextAsync(Guid tenantId, CancellationToken ct = default)
    {
        var sequence = await context.Set<TSequence>().FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        if (sequence is null)
        {
            sequence = TSequence.Create(tenantId);
            await context.Set<TSequence>().AddAsync(sequence, ct);
        }

        return sequence.Allocate();
    }
}
