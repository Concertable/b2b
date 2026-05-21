namespace Concertable.Concert.Application.Workflow;

internal interface IConcertTransitionValidator
{
    bool CanTransitionTo(ConcertStage from, ConcertStage to);
}
