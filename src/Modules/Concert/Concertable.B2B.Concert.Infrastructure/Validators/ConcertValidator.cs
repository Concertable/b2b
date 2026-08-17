using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Reunion.Validation;

namespace Concertable.B2B.Concert.Infrastructure.Validators;

internal sealed class ConcertValidator : IConcertValidator
{
    public ValidationResult CanUpdate(ConcertEntity concert, int newTotalTickets)
    {
        return newTotalTickets >= concert.TicketsSold
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(
                new ValidationErrors(
                    new Dictionary<string, string[]>
                    {
                        ["totalTickets"] =
                        [
                            $"Cannot reduce total tickets below the {concert.TicketsSold} already sold."
                        ]
                    }));
    }

    public ValidationResult CanPost(ConcertEntity concert, LifecycleState applicationState)
    {
        var errors = new List<KeyValuePair<string, string>>();

        if (applicationState != LifecycleState.Booked)
            errors.Add(new("booking", "Concert cannot be posted until the booking is confirmed"));

        if (concert.DatePosted is not null)
            errors.Add(new("datePosted", "Concert has already been posted"));

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(new ValidationErrors(errors));
    }
}
