using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Api.Errors;

internal sealed record ConcertLookupError : IError
{
    private ConcertLookupError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static ConcertLookupError ConcertNotFound(int concertId) =>
        NotFound("concert.get.not_found", $"Concert {concertId} was not found.");

    internal static ConcertLookupError ConcertByApplicationNotFound(int applicationId) =>
        NotFound("concert.get_by_application.not_found", $"No concert was found for application {applicationId}.");

    internal static ConcertLookupError ApplicationNotFound(int applicationId) =>
        NotFound("application.get.not_found", $"Application {applicationId} was not found.");

    internal static ConcertLookupError OpportunityNotFound(int opportunityId) =>
        NotFound("opportunity.get.not_found", $"Opportunity {opportunityId} was not found.");

    internal static ConcertLookupError ContractByApplicationNotFound(int applicationId) =>
        NotFound("contract.get_by_application.not_found", $"No contract was found for application {applicationId}.");

    internal static ConcertLookupError ContractByConcertNotFound(int concertId) =>
        NotFound("contract.get_by_concert.not_found", $"No contract was found for concert {concertId}.");

    internal static ConcertLookupError InvoiceByConcertNotFound(int concertId) =>
        NotFound("invoice.get_by_concert.not_found", $"No invoice was found for concert {concertId}.");

    private static ConcertLookupError NotFound(string code, string message) =>
        new(ErrorDefinition.NotFound(code, message));
}
