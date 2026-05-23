using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IFinishStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Finished;
    Task ExecuteAsync(int concertId);
}
