using Concertable.B2B.Concert.Domain.State;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record CancelConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) => ErrorDefinition.NotFound<ConcertNotFound>(
            $"Concert {concertId} was not found."),
        InvalidState(var state) => ErrorDefinition.Invalid<InvalidState>(
            $"A concert in {state} cannot be cancelled.")
    };

    [ErrorCode("concert.cancel.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.cancel.invalid_state")]
    public partial record InvalidState(ConcertState State);
}
