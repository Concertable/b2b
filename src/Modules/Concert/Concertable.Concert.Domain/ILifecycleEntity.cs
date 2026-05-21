using Concertable.Shared;

namespace Concertable.Concert.Domain;

internal interface ILifecycleEntity : IIdEntity
{
    ContractType ContractType { get; }
    ConcertStage CurrentStage { get; }
    void AdvanceStage(ConcertStage next);
}
