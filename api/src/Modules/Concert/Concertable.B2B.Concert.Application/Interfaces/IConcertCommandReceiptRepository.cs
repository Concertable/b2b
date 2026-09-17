using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertCommandReceiptRepository : IRepository<ConcertCommandReceipt, Guid>
{
    Task<ConcertCommandReceipt?> GetByTenantIdAndOperationAndRequestIdAsync(
        Guid issuedByTenantId, string operation, Guid requestId, CancellationToken ct = default);
}
