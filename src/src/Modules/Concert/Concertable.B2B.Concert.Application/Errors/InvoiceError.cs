using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record InvoiceError(ErrorDefinition Definition) : IError
{
    internal static InvoiceError ConcertNotFound(int concertId) =>
        new(ErrorDefinition.NotFound(
            "invoice.get_by_concert.not_found",
            $"No invoice was found for concert {concertId}."));
}
