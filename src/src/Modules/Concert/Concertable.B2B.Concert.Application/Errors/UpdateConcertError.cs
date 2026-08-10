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
                new Reunion.Errors.ValidationErrors(errors.ToDictionary()))
    };

    [ErrorCode("concert.update.not_found")]
    public partial record ConcertNotFound(int ConcertId);

    [ErrorCode("concert.update.invalid")]
    public partial record Invalid(ValidationErrors Errors);
}
