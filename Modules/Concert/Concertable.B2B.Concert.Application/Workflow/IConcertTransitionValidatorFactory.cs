namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IConcertTransitionValidatorFactory
{
    IConcertTransitionValidator Create(ContractType contractType);
}
