namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record UpdateConcertError : IError
{
    private UpdateConcertError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static UpdateConcertError NotFound(int concertId) =>
        new(ErrorDefinition.NotFound(
            "concert.update.not_found",
            $"Concert {concertId} was not found."));

    internal static UpdateConcertError Invalid(ValidationErrors errors) =>
        new(ErrorDefinition.Validation(
            "concert.update.invalid",
            "The concert update is invalid.",
            errors.ToDictionary()));
}
