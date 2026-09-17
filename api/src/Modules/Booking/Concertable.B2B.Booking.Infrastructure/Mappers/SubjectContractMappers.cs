using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Infrastructure.Mappers;

internal static class SubjectContractMappers
{
    extension(ContractEntity contract)
    {
        public SubjectContractDto ToSubjectContractDto() => new()
        {
            VenueName = contract.VenueName,
            ArtistName = contract.ArtistName,
            DealType = contract.DealType,
            CreatedAtUtc = contract.CreatedAtUtc,
        };
    }
}
