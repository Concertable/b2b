using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceRepository : IVenueArtistTenantScopedRepository<InvoiceEntity>
{
    /// <summary>
    /// Tenant-filtered: the invoice for the caller's own concert, or null for a non-party (the two-party
    /// filter hides it — the same 404-not-403 stance as reading the concert itself).
    /// </summary>
    Task<InvoiceEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default);

    /// <summary>Tenant-filtered: the invoice for the caller's own application, or null for a non-party.</summary>
    Task<InvoiceEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default);
}
