using Dunet;

namespace Concertable.B2B.Privacy.Domain.Lifecycle;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ErasureTransitionError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvalidTransition(var current, var trigger) =>
            ErrorDefinition.Conflict<InvalidTransition>(
                $"Cannot {trigger} a subject-erasure request from {current}."),
        ConcurrentRequest => ErrorDefinition.Conflict<ConcurrentRequest>(
            "Another attempt changed this subject-erasure request.")
    };

    [ErrorCode("privacy.erasure.invalid_state")]
    public partial record InvalidTransition(ErasureState Current, ErasureTrigger Trigger);

    [ErrorCode("privacy.erasure.concurrent_request")]
    public partial record ConcurrentRequest;
}
