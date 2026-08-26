using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface ISimpleAcceptStep : IConcertStep
{
    Task<UnitResult<AcceptApplicationError>> ExecuteAsync(
        ApplicationEntity application,
        CancellationToken ct = default);
}
