using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record ConcertError : IError
{
    private ConcertError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static ConcertError NotFound(int concertId) =>
        new(ErrorDefinition.NotFound(
            "concert.get.not_found",
            $"Concert {concertId} was not found."));

    internal static ConcertError ApplicationNotFound(int applicationId) =>
        new(ErrorDefinition.NotFound(
            "concert.get_by_application.not_found",
            $"No concert was found for application {applicationId}."));
}
