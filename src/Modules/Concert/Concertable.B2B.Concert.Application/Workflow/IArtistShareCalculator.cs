using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IArtistShareCalculator
{
    decimal Calculate(IContract contract, decimal totalRevenue);
}
