using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal static class LifecycleTransitionResultExtensions
{
    internal static async Task<ApplicationEntity> GetValueOrThrowAsync(
        this Task<Result<ApplicationEntity, LifecycleTransitionError>> resultTask)
    {
        var result = await resultTask;
        return result.Match(
            application => application,
            error => throw new InvalidOperationException(
                $"Internal lifecycle transition failed ({error.Definition.Code}): {error.Definition.Message}"));
    }
}
