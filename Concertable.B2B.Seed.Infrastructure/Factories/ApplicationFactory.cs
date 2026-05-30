using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Enums;
using Concertable.B2B.Contract.Contracts;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class ApplicationFactory
{
    public static StandardApplication Create(int artistId, int opportunityId)
        => StandardApplication.Create(artistId, opportunityId);

    public static StandardApplication Create(int artistId, int opportunityId, ContractType contractType)
    {
        var app = StandardApplication.Create(artistId, opportunityId, contractType);
        app.AdvanceStage(ConcertStage.Applied);
        return app;
    }

    public static PrepaidApplication CreatePrepaid(int artistId, int opportunityId, string paymentMethodId = "pm_card_visa")
        => PrepaidApplication.Create(artistId, opportunityId, paymentMethodId);

    public static PrepaidApplication CreatePrepaid(int artistId, int opportunityId, ContractType contractType, string paymentMethodId = "pm_card_visa")
    {
        var app = PrepaidApplication.Create(artistId, opportunityId, contractType, paymentMethodId);
        app.AdvanceStage(ConcertStage.Applied);
        return app;
    }

    public static StandardApplication Accepted(int artistId, int opportunityId, BookingEntity booking)
    {
        var app = New<StandardApplication>()
            .With(nameof(ApplicationEntity.ArtistId), artistId)
            .With(nameof(ApplicationEntity.OpportunityId), opportunityId)
            .With(nameof(ApplicationEntity.Status), ApplicationStatus.Accepted);
        app.Booking = booking;
        return app;
    }

    public static PrepaidApplication AcceptedPrepaid(int artistId, int opportunityId, BookingEntity booking, string paymentMethodId = "pm_card_visa")
    {
        var app = New<PrepaidApplication>()
            .With(nameof(ApplicationEntity.ArtistId), artistId)
            .With(nameof(ApplicationEntity.OpportunityId), opportunityId)
            .With(nameof(ApplicationEntity.Status), ApplicationStatus.Accepted)
            .With(nameof(PrepaidApplication.PaymentMethodId), paymentMethodId);
        app.Booking = booking;
        return app;
    }
}
