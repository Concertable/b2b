using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class ConcertMappers
{
    public static ConcertDto ToDto(this ConcertEntity concert) => new()
    {
        Id = concert.Id,
        Name = concert.Name,
        ImageUrl = concert.Booking.Application.Artist.Avatar,
        StartDate = concert.Booking.Application.Opportunity.Period.Start,
        EndDate = concert.Booking.Application.Opportunity.Period.End,
        County = concert.Booking.Application.Opportunity.Venue.Address.County,
        Town = concert.Booking.Application.Opportunity.Venue.Address.Town,
        DatePosted = concert.DatePosted
    };
}
