using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal interface IInvoicePrivilegedReadRepository
{
    Task<InvoiceEntity?> GetByIdAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<InvoiceEntity?> GetByConcertIdAsync(
        int concertId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
}
