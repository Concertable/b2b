using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertTransitionValidator
{
    bool CanTransitionTo(ConcertStage from, ConcertStage to);
}
