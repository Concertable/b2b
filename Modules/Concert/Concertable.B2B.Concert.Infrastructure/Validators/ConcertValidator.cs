using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Enums;
using FluentResults;

namespace Concertable.B2B.Concert.Infrastructure.Validators;

internal class ConcertValidator : IConcertValidator
{
    public Result CanUpdate(ConcertEntity concert, int newTotalTickets)
    {
        return newTotalTickets >= concert.TicketsSold
            ? Result.Ok()
            : Result.Fail($"Cannot reduce total tickets below the {concert.TicketsSold} already sold.");
    }

    public Result CanPost(ConcertEntity concert)
    {
        var errors = new List<string>();

        if (concert.Booking.Status != BookingStatus.Confirmed)
            errors.Add("Concert cannot be posted until the booking is confirmed");

        if (concert.DatePosted is not null)
            errors.Add("Concert has already been posted");

        return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
    }
}
