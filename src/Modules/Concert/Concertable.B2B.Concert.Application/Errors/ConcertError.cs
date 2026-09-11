using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var concertId) =>
            ErrorDefinition.NotFound<NotFound>($"Concert {concertId} was not found."),
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>(
                $"No concert was found for application {applicationId}."),
        MissingVenue =>
            ErrorDefinition.Forbidden<MissingVenue>("You must have a venue account."),
        MissingArtist =>
            ErrorDefinition.Forbidden<MissingArtist>("You must have an artist account.")
    };

    [ErrorCode("concert.get.not_found")]
    public partial record NotFound(int ConcertId);

    [ErrorCode("concert.get_by_application.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("concert.query.missing_venue")]
    public partial record MissingVenue;

    [ErrorCode("concert.query.missing_artist")]
    public partial record MissingArtist;
}
