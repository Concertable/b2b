namespace Concertable.Concert.Application.Workflow.Steps;

internal interface IPaidAcceptStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Accepted;
    Task ExecuteAsync(int applicationId, string paymentMethodId);
}
