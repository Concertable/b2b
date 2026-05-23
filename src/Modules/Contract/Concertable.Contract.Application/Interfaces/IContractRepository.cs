using Concertable.Contract.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.Contract.Application.Interfaces;

internal interface IContractRepository : IIdRepository<ContractEntity>
{
    Task<IEnumerable<ContractEntity>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
