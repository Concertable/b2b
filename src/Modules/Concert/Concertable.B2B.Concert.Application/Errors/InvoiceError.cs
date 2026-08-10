using Reunion.Errors;
using Dunet;

namespace Concertable.B2B.Concert.Application.Errors;

[Union(EnableImplicitConversions = false)]
internal abstract partial record InvoiceError : IError
{
    public ErrorDefinition Definition => this switch
    {
        ConcertNotFound(var concertId) =>
            ErrorDefinition.NotFound<ConcertNotFound>(
                $"No invoice was found for concert {concertId}.")
    };

    [ErrorCode("invoice.get_by_concert.not_found")]
    public partial record ConcertNotFound(int ConcertId);
}
