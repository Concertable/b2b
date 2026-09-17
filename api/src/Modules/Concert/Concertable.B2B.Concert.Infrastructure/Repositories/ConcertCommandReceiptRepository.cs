using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertCommandReceiptRepository
    : GuidRepository<ConcertCommandReceipt>, IConcertCommandReceiptRepository
{
    private readonly ConcertDbContext context;

    public ConcertCommandReceiptRepository(ConcertDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<ConcertCommandReceipt?> GetByTenantIdAndOperationAndRequestIdAsync(
        Guid issuedByTenantId, string operation, Guid requestId, CancellationToken ct = default) =>
        context.ConcertCommandReceipts.SingleOrDefaultAsync(
            receipt => receipt.IssuedByTenantId == issuedByTenantId
                       && receipt.Operation == operation
                       && receipt.RequestId == requestId,
            ct);
}
