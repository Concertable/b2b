using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Locking;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ConcertCommandReceiptRepository
    : GuidPrivilegedRepository<ConcertCommandReceipt>, IConcertCommandReceiptRepository
{
    private readonly ConcertPrivilegedDbContext context;

    public ConcertCommandReceiptRepository(ConcertPrivilegedDbContext context) : base(context)
    {
        this.context = context;
    }

    public void Add(ConcertCommandReceipt receipt) =>
        context.ConcertCommandReceipts.Add(receipt);

    public async Task<ConcertCommandReceipt?> GetByRequestWithRowLockAsync(
        Guid issuedByTenantId,
        string operation,
        Guid requestId,
        CancellationToken ct = default)
    {
        await context.ConcertCommandReceipts.LockRowsAsync(
            receipt => receipt.IssuedByTenantId == issuedByTenantId
                       && receipt.Operation == operation
                       && receipt.RequestId == requestId,
            RowLock.NoKeyUpdate,
            ct);
        return await context.ConcertCommandReceipts.SingleOrDefaultAsync(
            receipt => receipt.IssuedByTenantId == issuedByTenantId
                       && receipt.Operation == operation
                       && receipt.RequestId == requestId,
            ct);
    }
}
