using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Application.Contracts.Events;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Payment.Contracts.Events;
using Concertable.Payment.Contracts;
using Concertable.Customer.Ticket.Contracts.Events;
using Concertable.Shared.Email.Application;
using B2BPayoutOwnerRegisteredEvent = Concertable.B2B.Tenant.Contracts.Events.PayoutOwnerRegisteredEvent;

namespace Concertable.B2B.Hosting;

public static class B2BTopology
{
    extension(AsbTopology topology)
    {
        public AsbTopology AddB2BTopology()
        {
            topology.WithService(B2BConstants.ServiceName)
                    .Publish<ArtistChangedEvent>()
                    .Publish<ArtistRatingUpdatedEvent>()
                    .Publish<VenueChangedEvent>()
                    .Publish<VenueRatingUpdatedEvent>()
                    .Publish<ConcertChangedEvent>()
                    .Publish<ConcertPostedEvent>()
                    .Publish<ConcertRatingUpdatedEvent>()
                    .Publish<BookingCancelledEvent>()
                    .Publish<ConcertCancelledEvent>()
                    .Publish<ConcertCreatedEvent>()
                    .Publish<B2BPayoutOwnerRegisteredEvent>()
                    .Publish<TenantActivityRecordedEvent>()
                    .Publish<ApplicationAcceptedEvent>()
                    .Publish<BookingConfirmedEvent>()
                    .Subscribe<CustomerReviewSubmittedEvent>()
                    .Subscribe<CredentialRegisteredEvent>()
                    .Subscribe<PaymentSucceededEvent>()
                    .Subscribe<TicketPurchasedEvent>()
                    .Subscribe<PaymentFailedEvent>()
                    .Subscribe<CaptureEscrowSucceededEvent>()
                    .Subscribe<CaptureEscrowRejectedEvent>()
                    .Subscribe<DepositEscrowSucceededEvent>()
                    .Subscribe<DepositEscrowRejectedEvent>()
                    .Subscribe<RefundEscrowSucceededEvent>()
                    .Subscribe<RefundEscrowDeferredEvent>()
                    .Subscribe<RefundEscrowRejectedEvent>()
                    .Subscribe<ApplicationAcceptedEvent>()
                    .Subscribe<BookingConfirmedEvent>()
                    .Queue<SendEmailCommand>()
                    .Queue<NotifyConcertDraftCreatedCommand>();

            return topology;
        }
    }
}
