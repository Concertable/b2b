using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Payment.Contracts.Events;
using Concertable.Payment.Contracts;
using Concertable.Shared.Email.Application;
using B2BPayoutOwnerRegisteredEvent = Concertable.B2B.Tenant.Contracts.Events.PayoutOwnerRegisteredEvent;

namespace Concertable.B2B.Hosting;

public static class B2BTopology
{
    public static AsbTopology AddB2BTopology(this AsbTopology topology) =>
        topology
            .Publish<ArtistChangedEvent>()
            .Publish<ArtistRatingUpdatedEvent>()
            .Publish<VenueChangedEvent>()
            .Publish<VenueRatingUpdatedEvent>()
            .Publish<ConcertChangedEvent>()
            .Publish<ConcertPostedEvent>()
            .Publish<ConcertRatingUpdatedEvent>()
            .Publish<B2BPayoutOwnerRegisteredEvent>()
            .Publish<TenantActivityRecordedEvent>()
            .ForService(B2BConstants.ServiceName)
            .Subscribe<CustomerReviewSubmittedEvent>()
            .Subscribe<CredentialRegisteredEvent>()
            .Subscribe<PaymentSucceededEvent>()
            .Subscribe<PaymentFailedEvent>()
            .Subscribe<CaptureEscrowSucceededEvent>()
            .Subscribe<CaptureEscrowRejectedEvent>()
            .Subscribe<DepositEscrowSucceededEvent>()
            .Subscribe<DepositEscrowRejectedEvent>()
            .Subscribe<RefundEscrowSucceededEvent>()
            .Subscribe<RefundEscrowRejectedEvent>()
            .Subscribe<RefundEscrowDeferredEvent>()
            .Queue<SendEmailCommand>();
}
