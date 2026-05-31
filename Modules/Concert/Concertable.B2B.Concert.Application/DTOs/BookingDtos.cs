namespace Concertable.B2B.Concert.Application.DTOs;

internal interface IBooking
{
    int Id { get; }
}

internal record StandardBookingDto(int Id) : IBooking;

internal record DeferredBookingDto(int Id, string PaymentMethodId) : IBooking;

internal record BookingSettlement(int BookingId, string PaymentMethodId, Guid VenueUserId, Guid ArtistUserId);
