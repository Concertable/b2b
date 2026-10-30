using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal interface IInvoicePrivilegedReadRepository
{
    Task<InvoiceEntity?> GetByIdAsync(
        int id, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
    Task<InvoiceEntity?> GetByConcertIdAsync(
        int concertId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default);
}
