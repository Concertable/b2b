using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Booking.Contracts;

public sealed record SubjectContractDto
{
    public required DealType DealType { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
