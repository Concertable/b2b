using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class BookingFactory
{
    public static BookingSeedAggregate Create(
        int id,
        AcceptedApplication application,
        DateTime createdAtUtc,
        bool confirmed)
    {
        BookingEntity booking = application switch
        {
            FlatFeeAcceptedApplication or VenueHireAcceptedApplication =>
                StandardBooking.Create(application),
            DoorSplitAcceptedApplication doorSplit =>
                DeferredBooking.Create(application, doorSplit.PaymentMethodId),
            VersusAcceptedApplication versus =>
                DeferredBooking.Create(application, versus.PaymentMethodId),
            _ => throw new ArgumentOutOfRangeException(nameof(application), application, null)
        };
        booking.WithId(id);

        var contract = ContractEntity.Create(id, application, createdAtUtc)
            .WithId(id)
            .With(nameof(ContractEntity.PdfBlobName), $"contracts/{id}-seed.pdf");

        if (confirmed)
        {
            booking.RecordFinancialConfirmation($"seed-financial-{id}");
            booking.ClearDomainEvents();
        }

        return new BookingSeedAggregate(booking, contract);
    }
}

public sealed record BookingSeedAggregate(
    BookingEntity Booking,
    ContractEntity Contract);
