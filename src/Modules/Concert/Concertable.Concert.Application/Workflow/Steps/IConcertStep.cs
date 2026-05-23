using Concertable.Concert.Domain.Enums;

namespace Concertable.Concert.Application.Workflow.Steps;

internal interface IConcertStep
{
    static abstract ConcertStage Stage { get; }
}
