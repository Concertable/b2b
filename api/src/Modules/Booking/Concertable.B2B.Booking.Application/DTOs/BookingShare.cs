using Concertable.B2B.Booking.Contracts.Enums;

namespace Concertable.B2B.Booking.Application.DTOs;

internal sealed record ShareBookingRequest
{
    public Guid ToTenantId { get; init; }
    public Guid? ToMemberUserId { get; init; }
    public BookingAccessScope Scope { get; init; }
    public DateTime? ValidUntil { get; init; }
}

internal sealed record BookingShareResponse
{
    public Guid GrantId { get; init; }
    public Guid ToTenantId { get; init; }
    public Guid? ToMemberUserId { get; init; }
    public BookingAccessScope Scope { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime? ValidUntil { get; init; }
}
