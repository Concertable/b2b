namespace Concertable.B2B.Concert.Application.DTOs;

internal interface IBooking
{
    int Id { get; }
}

internal sealed record StandardBookingDto(int Id) : IBooking;

internal sealed record DeferredBookingDto(int Id, string PaymentMethodId) : IBooking;

/* The frozen-at-accept party snapshot settlement pays between — never live tenant state. */
internal sealed record BookingSettlement(int BookingId, string PaymentMethodId, Guid VenueTenantId, Guid ArtistTenantId);
