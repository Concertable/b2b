using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record ContractError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ApplicationNotFound(var applicationId) =>
            ErrorDefinition.NotFound<ApplicationNotFound>(
                $"No contract was found for application {applicationId}."),
        ConcertNotFound(var concertId) =>
            ErrorDefinition.NotFound<ConcertNotFound>(
                $"No contract was found for concert {concertId}.")
    };

    [ErrorCode("contract.get_by_application.not_found")]
    public partial record ApplicationNotFound(int ApplicationId);

    [ErrorCode("contract.get_by_concert.not_found")]
    public partial record ConcertNotFound(int ConcertId);
}
