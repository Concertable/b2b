using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class InvoicePrivilegedReadRepository(ConcertPrivilegedDbContext context)
    : IInvoicePrivilegedReadRepository
{
    public Task<InvoiceEntity?> GetByIdAsync(
        int id, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        Root(permission, actor, now)
            .SingleOrDefaultAsync(invoice => invoice.Id == id, ct);

    public Task<InvoiceEntity?> GetByConcertIdAsync(
        int concertId, TenantPermission permission, MembershipSnapshot actor,
        DateTimeOffset now, CancellationToken ct = default) =>
        Root(permission, actor, now)
            .SingleOrDefaultAsync(invoice => invoice.ConcertId == concertId, ct);

    private IQueryable<InvoiceEntity> Root(
        TenantPermission permission, MembershipSnapshot actor, DateTimeOffset now) =>
        context.Invoices.AsNoTracking()
            .AccessibleBy(actor, permission, InvoiceAccessScope.Read, now.UtcDateTime)
            .WhereCurrentMember(context.MembershipAuthority, actor);
}
