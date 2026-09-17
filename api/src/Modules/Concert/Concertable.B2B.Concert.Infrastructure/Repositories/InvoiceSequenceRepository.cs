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

    public Task<InvoiceSequenceEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.InvoiceSequences.SingleOrDefaultAsync(sequence => sequence.TenantId == tenantId, ct);

    public async Task InsertAsync(InvoiceSequenceEntity sequence, CancellationToken ct = default) =>
        await context.InvoiceSequences.AddAsync(sequence, ct);
}
