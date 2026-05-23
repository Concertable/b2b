using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IConcertStep
{
    static abstract ConcertStage Stage { get; }
}
