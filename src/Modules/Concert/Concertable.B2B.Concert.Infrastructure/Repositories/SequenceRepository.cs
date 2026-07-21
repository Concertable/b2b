using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class SequenceRepository<TSequence> : BaseRepository<TSequence>, ISequenceRepository<TSequence>
    where TSequence : class, ISequence<TSequence>
{
    public SequenceRepository(ConcertDbContext context) : base(context) { }

    public async Task<long> AllocateNextAsync(Guid tenantId, CancellationToken ct = default)
    {
        var sequence = await context.Set<TSequence>().FirstOrDefaultAsync(s => s.TenantId == tenantId, ct)
            ?? await base.AddAsync(TSequence.Create(tenantId), ct);

        return sequence.Allocate();
    }
}
