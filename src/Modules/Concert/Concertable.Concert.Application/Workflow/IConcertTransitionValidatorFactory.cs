namespace Concertable.Concert.Application.Workflow;

internal interface IConcertTransitionValidatorFactory
{
    IConcertTransitionValidator Create(ContractType contractType);
}
