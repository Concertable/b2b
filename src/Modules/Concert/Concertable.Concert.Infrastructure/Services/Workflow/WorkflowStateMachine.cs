using Concertable.Concert.Application.Workflow;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Services.Workflow;

internal class WorkflowStateMachine<TEntity> : IWorkflowStateMachine<TEntity> where TEntity : class, ILifecycleEntity
{
    private readonly ILifecycleRepository<TEntity> repository;
    private readonly IConcertTransitionValidatorFactory validators;

    public WorkflowStateMachine(
        ILifecycleRepository<TEntity> repository,
        IConcertTransitionValidatorFactory validators)
    {
        this.repository = repository;
        this.validators = validators;
    }

    public async Task<TEntity> TransitionAsync(ConcertStage target, CreateStep<TEntity> create)
    {
        var entity = await create();
        Guard(entity, target);
        entity.AdvanceStage(target);
        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
        return entity;
    }

    public async Task TransitionAsync(int entityId, ConcertStage target, ExecuteStep<TEntity> execute)
    {
        var entity = await LoadAsync(entityId);
        Guard(entity, target);
        await execute(entity);
        entity.AdvanceStage(target);
        await repository.SaveChangesAsync();
    }

    private void Guard(TEntity entity, ConcertStage target)
    {
        var validator = validators.Create(entity.ContractType);
        if (!validator.CanTransitionTo(entity.CurrentStage, target))
            throw new ConflictException($"Cannot transition from {entity.CurrentStage} to {target}");
    }

    private async Task<TEntity> LoadAsync(int entityId)
        => await repository.GetByIdAsync(entityId)
            ?? throw new NotFoundException($"{typeof(TEntity).Name} {entityId} not found");
}
