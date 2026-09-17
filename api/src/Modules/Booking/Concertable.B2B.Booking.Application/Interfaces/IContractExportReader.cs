using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Booking.Application.Interfaces;

/// <summary>Reads the subject's portable contract fragment — the RETAINED contracts their tenants are party
/// to — for a GDPR access/portability export. Read-only.</summary>
internal interface IContractExportReader
{
    Task<IReadOnlyList<ContractExport>> GetContractExportsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
