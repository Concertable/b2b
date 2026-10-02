using Concertable.B2B.Concert.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISubjectRecordReader
{
    Task<SubjectConcertRecordsDto> GetSubjectRecordsAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
