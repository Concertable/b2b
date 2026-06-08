using Concertable.B2B.Concert.Domain.Entities;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class BookingFactory
{
    public static StandardBooking Standard(int id)
        => New<StandardBooking>()
            .With("Id", id);

    public static DeferredBooking Deferred(int id, string paymentMethodId = "pm_card_visa")
        => New<DeferredBooking>()
            .With("Id", id)
            .With(nameof(DeferredBooking.PaymentMethodId), paymentMethodId);
}
