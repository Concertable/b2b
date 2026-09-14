using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>Assembles the subject's portable Concert records fragment — the RETAINED invoices, contracts and
/// self-billing agreements their tenants are party to — for a GDPR access/portability export. Read-only.</summary>
internal interface IConcertRecordsExporter
{
    Task<ConcertRecordsExport> ExportAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);
}
