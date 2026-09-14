using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IContractPdfRenderer
{
    Task<byte[]> GetOrCreateAsync(ContractEntity contract, CancellationToken ct = default);
}
