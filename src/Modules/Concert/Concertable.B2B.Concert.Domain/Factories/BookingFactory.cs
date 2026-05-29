using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Enums;
using static Concertable.Seeding.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Concert.Domain.Factories;

public static class BookingFactory
{
    public static StandardBooking Confirmed(int id)
        => New<StandardBooking>()
            .With("Id", id)
            .With(nameof(BookingEntity.Status), BookingStatus.Confirmed);

    public static DeferredBooking ConfirmedDeferred(int id, string paymentMethodId = "pm_card_visa")
        => New<DeferredBooking>()
            .With("Id", id)
            .With(nameof(BookingEntity.Status), BookingStatus.Confirmed)
            .With(nameof(DeferredBooking.PaymentMethodId), paymentMethodId);

    public static StandardBooking AwaitingPayment(int id)
        => New<StandardBooking>()
            .With("Id", id)
            .With(nameof(BookingEntity.Status), BookingStatus.AwaitingPayment);

    public static StandardBooking Complete(int id)
        => New<StandardBooking>()
            .With("Id", id)
            .With(nameof(BookingEntity.Status), BookingStatus.Complete);

    public static DeferredBooking CompleteDeferred(int id, string paymentMethodId = "pm_card_visa")
        => New<DeferredBooking>()
            .With("Id", id)
            .With(nameof(BookingEntity.Status), BookingStatus.Complete)
            .With(nameof(DeferredBooking.PaymentMethodId), paymentMethodId);
}
