using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IContractAccessor
{
    IContract Contract { get; }
}
