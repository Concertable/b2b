using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Workflow;

internal interface IArtistShareCalculator
{
    decimal Calculate(IDeal contract, decimal totalRevenue);
}
