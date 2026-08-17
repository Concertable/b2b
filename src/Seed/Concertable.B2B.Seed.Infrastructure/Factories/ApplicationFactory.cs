using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.Deal.Contracts;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class ApplicationFactory
{
    public static StandardApplication Create(int artistId, int opportunityId)
        => New<StandardApplication>()
            .With(nameof(ApplicationEntity.ArtistId), artistId)
            .With(nameof(ApplicationEntity.OpportunityId), opportunityId);

    public static StandardApplication Create(int artistId, int opportunityId, DealType dealType)
        => Create(artistId, opportunityId)
            .With(nameof(ApplicationEntity.DealType), dealType);

    public static PrepaidApplication CreatePrepaid(int artistId, int opportunityId, string paymentMethodId = "pm_card_visa")
        => New<PrepaidApplication>()
            .With(nameof(ApplicationEntity.ArtistId), artistId)
            .With(nameof(ApplicationEntity.OpportunityId), opportunityId)
            .With(nameof(PrepaidApplication.PaymentMethodId), paymentMethodId);

    public static PrepaidApplication CreatePrepaid(int artistId, int opportunityId, DealType dealType, string paymentMethodId = "pm_card_visa")
        => CreatePrepaid(artistId, opportunityId, paymentMethodId)
            .With(nameof(ApplicationEntity.DealType), dealType);

    public static StandardApplication Accepted(int artistId, int opportunityId)
        => InState<StandardApplication>(artistId, opportunityId, ApplicationState.Accepted);

    public static PrepaidApplication AcceptedPrepaid(int artistId, int opportunityId, string paymentMethodId = "pm_card_visa")
        => InState<PrepaidApplication>(artistId, opportunityId, ApplicationState.Accepted)
            .With(nameof(PrepaidApplication.PaymentMethodId), paymentMethodId);

    public static StandardApplication Booked(int artistId, int opportunityId)
        => InState<StandardApplication>(artistId, opportunityId, ApplicationState.Accepted);

    public static PrepaidApplication BookedPrepaid(int artistId, int opportunityId, string paymentMethodId = "pm_card_visa")
        => InState<PrepaidApplication>(artistId, opportunityId, ApplicationState.Accepted)
            .With(nameof(PrepaidApplication.PaymentMethodId), paymentMethodId);

    public static StandardApplication Complete(int artistId, int opportunityId)
        => InState<StandardApplication>(artistId, opportunityId, ApplicationState.Accepted);

    public static PrepaidApplication CompletePrepaid(int artistId, int opportunityId, string paymentMethodId = "pm_card_visa")
        => InState<PrepaidApplication>(artistId, opportunityId, ApplicationState.Accepted)
            .With(nameof(PrepaidApplication.PaymentMethodId), paymentMethodId);

    private static TApplication InState<TApplication>(int artistId, int opportunityId, ApplicationState state)
        where TApplication : ApplicationEntity =>
        New<TApplication>()
            .With(nameof(ApplicationEntity.ArtistId), artistId)
            .With(nameof(ApplicationEntity.OpportunityId), opportunityId)
            .With(nameof(ApplicationEntity.State), state);
}
