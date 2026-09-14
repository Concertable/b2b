using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Booking.Application.Interfaces;

/// <summary>Assembles the subject's portable contract fragment — the RETAINED contracts their tenants are party
/// to — for a GDPR access/portability export. Read-only.</summary>
internal interface IContractExporter
{
    Task<IReadOnlyList<ContractExport>> ExportAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);
}
