using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record UpdateConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) =>
            ErrorDefinition.NotFound<ConcertNotFound>(
                $"Concert {concertId} was not found."),
        Invalid(var errors) =>
            ErrorDefinition.Validation<Invalid>(
                "The concert update is invalid.",
                errors),
        Superseded(var concertId) => ErrorDefinition.Conflict<Superseded>(
            $"Concert {concertId} changed while this update was in flight.")
    };

    [ErrorCode("concert.update.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.update.invalid")]
    public partial record Invalid(ValidationErrors Errors);

    [ErrorCode("concert.update.superseded")]
    public partial record Superseded(int ConcertId);
}
