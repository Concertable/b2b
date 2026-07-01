using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class LifecycleTransitioner : ILifecycleTransitioner
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IConcertStateMachineRegistry machines;

    public LifecycleTransitioner(
        IApplicationRepository applicationRepository,
        IConcertStateMachineRegistry machines)
    {
        this.applicationRepository = applicationRepository;
        this.machines = machines;
    }

    public async Task<ApplicationEntity> TransitionAsync(int applicationId, Trigger trigger, TransitionEffect? effect = null)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId)
            ?? throw new NotFoundException("Application not found");

        var machine = machines.Get(application.ContractType);
        machine.Next(application.State, trigger);

        if (effect is not null)
            await effect(application);

        application.Transition(trigger, machine);
        await applicationRepository.SaveChangesAsync();
        return application;
    }
}
