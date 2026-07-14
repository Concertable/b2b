using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Deal.Contracts;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class ApplicationFactory
{
    public static StandardApplication Create(int artistId, int opportunityId)
        => StandardApplication.Create(artistId, opportunityId);

    public static StandardApplication Create(int artistId, int opportunityId, DealType dealType)
        => StandardApplication.Create(artistId, opportunityId, dealType);

    public static PrepaidApplication CreatePrepaid(int artistId, int opportunityId, string paymentMethodId = "pm_card_visa")
        => PrepaidApplication.Create(artistId, opportunityId, paymentMethodId);

    public static PrepaidApplication CreatePrepaid(int artistId, int opportunityId, DealType dealType, string paymentMethodId = "pm_card_visa")
        => PrepaidApplication.Create(artistId, opportunityId, dealType, paymentMethodId);

    public static StandardApplication Accepted(int artistId, int opportunityId, BookingEntity booking)
        => InState<StandardApplication>(artistId, opportunityId, booking, LifecycleState.Accepted);

    public static PrepaidApplication AcceptedPrepaid(int artistId, int opportunityId, BookingEntity booking, string paymentMethodId = "pm_card_visa")
        => InState<PrepaidApplication>(artistId, opportunityId, booking, LifecycleState.Accepted)
            .With(nameof(PrepaidApplication.PaymentMethodId), paymentMethodId);

    public static StandardApplication Booked(int artistId, int opportunityId, BookingEntity booking)
        => InState<StandardApplication>(artistId, opportunityId, booking, LifecycleState.Booked);

    public static PrepaidApplication BookedPrepaid(int artistId, int opportunityId, BookingEntity booking, string paymentMethodId = "pm_card_visa")
        => InState<PrepaidApplication>(artistId, opportunityId, booking, LifecycleState.Booked)
            .With(nameof(PrepaidApplication.PaymentMethodId), paymentMethodId);

    public static StandardApplication Complete(int artistId, int opportunityId, BookingEntity booking)
        => InState<StandardApplication>(artistId, opportunityId, booking, LifecycleState.Complete);

    public static PrepaidApplication CompletePrepaid(int artistId, int opportunityId, BookingEntity booking, string paymentMethodId = "pm_card_visa")
        => InState<PrepaidApplication>(artistId, opportunityId, booking, LifecycleState.Complete)
            .With(nameof(PrepaidApplication.PaymentMethodId), paymentMethodId);

    private static TApplication InState<TApplication>(int artistId, int opportunityId, BookingEntity booking, LifecycleState state)
        where TApplication : ApplicationEntity
    {
        var app = New<TApplication>()
            .With(nameof(ApplicationEntity.ArtistId), artistId)
            .With(nameof(ApplicationEntity.OpportunityId), opportunityId)
            .With(nameof(ApplicationEntity.State), state);
        app.Accept(booking);
        return app;
    }
}
