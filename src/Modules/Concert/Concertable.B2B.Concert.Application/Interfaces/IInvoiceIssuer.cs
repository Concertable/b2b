using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceIssuer
{
    Task IssueAsync(ConcertEntity concert, CancellationToken ct = default);
}
