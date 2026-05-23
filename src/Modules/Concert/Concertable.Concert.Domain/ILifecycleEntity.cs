using Concertable.Concert.Domain.Enums;
using Concertable.Kernel;

namespace Concertable.Concert.Domain;

internal interface ILifecycleEntity : IIdEntity
{
    ContractType ContractType { get; }
    ConcertStage CurrentStage { get; }
    void AdvanceStage(ConcertStage next);
}
