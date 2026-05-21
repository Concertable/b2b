using Concertable.Concert.Application.Workflow.Steps;

namespace Concertable.Concert.Application.Workflow.Capabilities;

internal interface IAppliesPaid : IApplies
{
    IPaidApplyStep Apply { get; }
}
