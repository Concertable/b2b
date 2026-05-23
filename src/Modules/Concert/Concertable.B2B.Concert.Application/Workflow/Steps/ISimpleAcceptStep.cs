using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface ISimpleAcceptStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Accepted;
    Task ExecuteAsync(int applicationId);
}
