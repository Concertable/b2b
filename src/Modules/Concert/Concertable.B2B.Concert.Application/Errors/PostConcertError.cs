using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record PostConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) =>
            ErrorDefinition.NotFound<ConcertNotFound>(
                $"Concert {concertId} was not found."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The concert cannot be posted.",
                errors),
        InvalidTransition(var error) => ErrorDefinition.Conflict<InvalidTransition>(
            $"A concert in {error.Current} cannot be posted.")
    };

    [ErrorCode("concert.post.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.post.invalid")]
    public partial record Invalid(ValidationErrors Errors);

    public partial record InvalidTransition(TransitionError<State, Trigger> Error);
}
