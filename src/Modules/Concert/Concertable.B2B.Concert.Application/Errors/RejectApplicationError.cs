using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record RejectApplicationError : IError
{
    private RejectApplicationError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static RejectApplicationError FromLifecycle(LifecycleTransitionError error) =>
        error.Definition.Kind is ErrorKind.NotFound
            ? new RejectApplicationError(ErrorDefinition.NotFound(
                "application.reject.not_found",
                error.Definition.Message))
            : new RejectApplicationError(ErrorDefinition.Conflict(
                "application.reject.invalid_transition",
                error.Definition.Message));
}
