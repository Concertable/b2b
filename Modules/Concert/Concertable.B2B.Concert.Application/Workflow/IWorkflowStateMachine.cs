using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.Workflow;

internal delegate Task<TEntity> CreateStep<TEntity>() where TEntity : class, ILifecycleEntity;
internal delegate Task ExecuteStep<TEntity>(TEntity entity) where TEntity : class, ILifecycleEntity;

internal interface IWorkflowStateMachine<TEntity> where TEntity : class, ILifecycleEntity
{
    Task<TEntity> TransitionAsync(ConcertStage target, CreateStep<TEntity> create);

    Task TransitionAsync(int entityId, ConcertStage target, ExecuteStep<TEntity> execute);
}
