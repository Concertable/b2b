namespace Concertable.Concert.Application.Workflow.Steps;

internal interface ISimpleApplyStep : IConcertStep
{
    static ConcertStage IConcertStep.Stage => ConcertStage.Applied;
    Task<ApplicationEntity> ApplyAsync(int artistId, int opportunityId, ContractType contractType);
}
