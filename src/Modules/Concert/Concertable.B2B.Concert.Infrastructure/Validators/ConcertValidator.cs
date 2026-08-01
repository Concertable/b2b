using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;

namespace Concertable.B2B.Concert.Infrastructure.Validators;

internal sealed class ConcertValidator : IConcertValidator
{
    public UnitResult<ValidationErrors> CanUpdate(ConcertEntity concert, int newTotalTickets)
    {
        return newTotalTickets >= concert.TicketsSold
            ? UnitResult.Success<ValidationErrors>()
            : UnitResult.Failure(
                new ValidationErrors(
                    new Dictionary<string, string[]>
                    {
                        ["totalTickets"] =
                        [
                            $"Cannot reduce total tickets below the {concert.TicketsSold} already sold."
                        ]
                    }));
    }

    public UnitResult<ValidationErrors> CanPost(ConcertEntity concert)
    {
        var errors = new List<KeyValuePair<string, string>>();

        if (concert.Booking.Application.State != LifecycleState.Booked)
            errors.Add(new("booking", "Concert cannot be posted until the booking is confirmed"));

        if (concert.DatePosted is not null)
            errors.Add(new("datePosted", "Concert has already been posted"));

        return errors.Count == 0
            ? UnitResult.Success<ValidationErrors>()
            : UnitResult.Failure(new ValidationErrors(errors));
    }
}
