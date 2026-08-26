using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IPaidAcceptStep : IConcertStep
{
    Task<UnitResult<AcceptApplicationError>> ExecuteAsync(
        ApplicationEntity application,
        string paymentMethodId,
        CancellationToken ct = default);
}
