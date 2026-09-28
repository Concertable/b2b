using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertCommandReceiptRepository : IRepository<ConcertCommandReceipt, Guid>
{
    void Add(ConcertCommandReceipt receipt);

    Task<ConcertCommandReceipt?> GetByTenantIdAndOperationAndRequestIdForUpdateAsync(
        Guid issuedByTenantId, string operation, Guid requestId, CancellationToken ct = default);
}
