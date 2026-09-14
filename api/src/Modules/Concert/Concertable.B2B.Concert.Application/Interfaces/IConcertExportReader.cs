using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>Reads the subject's portable Concert fragment — the RETAINED invoices, contracts and
/// self-billing agreements their tenants are party to — for a GDPR access/portability export. Read-only.</summary>
internal interface IConcertExportReader
{
    Task<ConcertExport> GetConcertExportAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);
}
