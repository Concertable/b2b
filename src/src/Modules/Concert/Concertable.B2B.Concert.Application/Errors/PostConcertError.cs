namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record PostConcertError : IError
{
    private PostConcertError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static PostConcertError NotFound(int concertId) =>
        new(ErrorDefinition.NotFound(
            "concert.post.not_found",
            $"Concert {concertId} was not found."));

    internal static PostConcertError Invalid(ValidationErrors errors) =>
        new(ErrorDefinition.Validation(
            "concert.post.invalid",
            "The concert cannot be posted.",
            errors.ToDictionary()));
}
