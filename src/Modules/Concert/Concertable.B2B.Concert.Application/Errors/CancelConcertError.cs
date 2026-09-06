using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CancelConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) => ErrorDefinition.NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        InvalidTransition(var error) => ErrorDefinition.Conflict<InvalidTransition>(
            $"A concert in {error.Current} cannot be cancelled."),
        Superseded(var concertId) => ErrorDefinition.Conflict<Superseded>(
            $"Concert {concertId} changed while this cancellation was in flight.")
    };

    [ErrorCode("concert.cancel.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.cancel.invalid_state")]
    public partial record InvalidTransition(TransitionError<ConcertState, ConcertTrigger> Error);

    [ErrorCode("concert.cancel.superseded")]
    public partial record Superseded(int ConcertId);
}
