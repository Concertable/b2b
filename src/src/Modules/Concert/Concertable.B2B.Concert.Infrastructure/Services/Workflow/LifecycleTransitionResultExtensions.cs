using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal static class LifecycleTransitionResultExtensions
{
    internal static async Task<ApplicationEntity> GetValueOrThrowAsync(
        this Task<Result<ApplicationEntity, LifecycleTransitionError>> resultTask)
    {
        var result = await resultTask;
        return result.Match(
            application => application,
            error => throw error.Definition.Kind switch
            {
                ErrorKind.NotFound => new NotFoundException(error.Definition.Message),
                ErrorKind.Conflict => new ConflictException(error.Definition.Message),
                _ => new InvalidOperationException(error.Definition.Message)
            });
    }
}
