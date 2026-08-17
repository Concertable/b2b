using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
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
            .Subscribe<CustomerReviewSubmittedEvent>(B2BConstants.ServiceName)
            .Subscribe<CredentialRegisteredEvent>(B2BConstants.ServiceName)
            .Subscribe<PaymentSucceededEvent>(B2BConstants.ServiceName)
            .Subscribe<PaymentFailedEvent>(B2BConstants.ServiceName)
            .Subscribe<CaptureEscrowSucceededEvent>(B2BConstants.ServiceName)
            .Subscribe<CaptureEscrowRejectedEvent>(B2BConstants.ServiceName)
            .Subscribe<DepositEscrowSucceededEvent>(B2BConstants.ServiceName)
            .Subscribe<DepositEscrowRejectedEvent>(B2BConstants.ServiceName)
            .Subscribe<RefundEscrowSucceededEvent>(B2BConstants.ServiceName)
            .Subscribe<RefundEscrowRejectedEvent>(B2BConstants.ServiceName)
            .Subscribe<RefundEscrowDeferredEvent>(B2BConstants.ServiceName)
            .Queue<SendEmailCommand>(B2BConstants.ServiceName);
}
