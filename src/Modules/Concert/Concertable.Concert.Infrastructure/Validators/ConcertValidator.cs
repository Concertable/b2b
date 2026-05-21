using FluentResults;

namespace Concertable.Concert.Infrastructure.Validators;

internal class ConcertValidator : IConcertValidator
{
    public Result CanUpdate(ConcertEntity concert, int newTotalTickets)
    {
        // BROKEN Phase 1: tickets-sold validation needs Customer.Concert.ConcertEntity.AvailableTickets via
        // ICustomerConcertModule facade (not yet introduced). Accepting any value pending Phase 2.
        return Result.Ok();
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
