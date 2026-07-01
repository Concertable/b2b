using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Application.Workflow.Capabilities;

internal interface IAppliesPaid : IApplies
{
    IPaidApplyStep Apply { get; }
}
