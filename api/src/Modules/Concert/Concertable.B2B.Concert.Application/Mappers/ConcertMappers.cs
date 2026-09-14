using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Domain.Errors;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Mappers;

internal static class ConcertMappers
{
    extension(ConcertEntity concert)
    {
        public ConcertDto ToDto() => new()
        {
            Id = concert.Id,
            Name = concert.Name,
            ImageUrl = concert.Artist.Avatar,
            StartDate = concert.Period.Start,
            EndDate = concert.Period.End,
            County = concert.Venue.Address.County,
            Town = concert.Venue.Address.Town,
            DatePosted = concert.DatePosted
        };
    }

    extension(DoorRevenueDeclarationError error)
    {
        public DeclareDoorRevenueError ToDeclareDoorRevenueError() => error switch
        {
            DoorRevenueDeclarationError.NegativeRevenue =>
                new DeclareDoorRevenueError.Negative()
        };
    }
}
