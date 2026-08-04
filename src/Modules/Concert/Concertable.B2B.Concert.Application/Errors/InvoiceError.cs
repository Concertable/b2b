using Concertable.Kernel.Errors;

namespace Concertable.B2B.Concert.Application.Errors;

internal sealed record InvoiceError : IError
{
    private InvoiceError(ErrorDefinition definition)
    {
        Definition = definition;
    }

    public ErrorDefinition Definition { get; }

    internal static InvoiceError ConcertNotFound(int concertId) =>
        new(ErrorDefinition.NotFound(
            "invoice.get_by_concert.not_found",
            $"No invoice was found for concert {concertId}."));
}
