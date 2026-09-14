using Dunet;

namespace Concertable.B2B.Privacy.Domain.Lifecycle;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ErasureTransitionError : IError
{
    public ErrorDefinition Definition => this switch
    {
        InvalidTransition(var current, var trigger) =>
            ErrorDefinition.Conflict<InvalidTransition>(
                $"Cannot {trigger} a subject-erasure request from {current}.")
    };

    [ErrorCode("privacy.erasure.invalid_transition")]
    public partial record InvalidTransition(ErasureState Current, ErasureTrigger Trigger);
}
