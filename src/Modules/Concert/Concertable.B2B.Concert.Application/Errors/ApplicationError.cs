using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record ApplicationError(ErrorDefinition Definition) : IError
{
    internal static ApplicationError NotFound(int applicationId) =>
        new(ErrorDefinition.NotFound(
            "application.get.not_found",
            $"Application {applicationId} was not found."));
}
