using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface ISubjectContractReader
{
    Task<IReadOnlyList<SubjectContractDto>> GetSubjectContractsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
