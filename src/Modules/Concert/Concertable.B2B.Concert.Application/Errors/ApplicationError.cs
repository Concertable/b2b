using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record ApplicationError : IError
{
    private ApplicationError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static ApplicationError NotFound(int applicationId) =>
        new(ErrorDefinition.NotFound(
            "application.get.not_found",
            $"Application {applicationId} was not found."));
}
