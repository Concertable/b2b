using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Workflow;

internal delegate Task TransitionEffect(ApplicationEntity application);

internal interface ILifecycleTransitioner
{
    Task<ApplicationEntity> TransitionAsync(int applicationId, Trigger trigger, TransitionEffect? effect = null);
}
