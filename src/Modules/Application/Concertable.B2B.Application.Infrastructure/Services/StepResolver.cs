using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class StepResolver<TStep>(IKeyedServiceProvider services) : IStepResolver<TStep>
    where TStep : class
{
    public TStep Resolve(DealType dealType) => services.GetRequiredKeyedService<TStep>(dealType);
}
