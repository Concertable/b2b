using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
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

    public async Task<ConcertCommandReceipt?> GetByTenantIdAndOperationAndRequestIdForUpdateAsync(
        Guid issuedByTenantId,
        string operation,
        Guid requestId,
        CancellationToken ct = default)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM concert."ConcertCommandReceipts"
            WHERE "IssuedByTenantId" = {issuedByTenantId}
              AND "Operation" = {operation}
              AND "RequestId" = {requestId}
            FOR UPDATE
            """, ct);
        return await context.ConcertCommandReceipts.SingleOrDefaultAsync(
            receipt => receipt.IssuedByTenantId == issuedByTenantId
                       && receipt.Operation == operation
                       && receipt.RequestId == requestId,
            ct);
    }
}
