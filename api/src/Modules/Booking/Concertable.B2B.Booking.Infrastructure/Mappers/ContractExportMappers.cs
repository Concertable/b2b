using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Infrastructure.Mappers;

internal static class ContractExportMappers
{
    extension(ContractEntity contract)
    {
        public ContractExport ToContractExport() => new()
        {
            VenueName = contract.VenueName,
            ArtistName = contract.ArtistName,
            DealType = contract.DealType.ToString(),
            CreatedAtUtc = contract.CreatedAtUtc,
        };
    }
}
