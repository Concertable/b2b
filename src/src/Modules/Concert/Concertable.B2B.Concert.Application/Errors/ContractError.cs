using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record ContractError(ErrorDefinition Definition) : IError
{
    internal static ContractError ApplicationNotFound(int applicationId) =>
        new(ErrorDefinition.NotFound(
            "contract.get_by_application.not_found",
            $"No contract was found for application {applicationId}."));

    internal static ContractError ConcertNotFound(int concertId) =>
        new(ErrorDefinition.NotFound(
            "contract.get_by_concert.not_found",
            $"No contract was found for concert {concertId}."));
}
