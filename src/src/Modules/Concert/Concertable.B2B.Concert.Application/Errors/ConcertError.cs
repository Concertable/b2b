using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ConcertError : IError
{
    public ErrorDefinition Definition => this switch
    {
        NotFound(var concertId) =>
            ErrorDefinition.For<ConcertError>().NotFound<NotFound>($"Concert {concertId} was not found."),
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.For<ConcertError>().NotFound<ApplicationNotFound>(
                $"No concert was found for application {applicationId}.")
    };

    [ErrorCode("concert.get.not_found")]
    public partial record NotFound(int ConcertId);

    [ErrorCode("concert.get_by_application.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);
}
