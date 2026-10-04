using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class InvoicePrivilegedReadRepository(ConcertPrivilegedDbContext context)
    : IInvoicePrivilegedReadRepository
{
    public Task<InvoiceEntity?> GetByIdAsync(
        int id, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        Root(binding, actor, now)
            .SingleOrDefaultAsync(invoice => invoice.Id == id, ct);

    public Task<InvoiceEntity?> GetByConcertIdAsync(
        int concertId, ResourcePolicyBinding binding, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        Root(binding, actor, now)
            .SingleOrDefaultAsync(invoice => invoice.ConcertId == concertId, ct);

    private IQueryable<InvoiceEntity> Root(
        ResourcePolicyBinding binding, MembershipSnapshot actor, DateTimeOffset now) =>
        context.Invoices.AsNoTracking().Where(
            ConcertGrantPolicy.Invoices(context, binding, actor, now))
            .Where(invoice => binding.Facet == ResourceFacet.Read);
}
