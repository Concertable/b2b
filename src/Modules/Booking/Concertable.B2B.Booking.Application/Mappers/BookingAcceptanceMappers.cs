using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.ValueObjects;

namespace Concertable.B2B.Booking.Application.Mappers;

internal static class BookingAcceptanceMappers
{
    extension(AcceptedApplication application)
    {
        public BookingAcceptance ToBookingAcceptance() => application switch
        {
            FlatFeeAcceptedApplication flatFee => new StandardBookingAcceptance(
                flatFee.OperationId, flatFee.ApplicationId, flatFee.OpportunityId, flatFee.ArtistId, flatFee.VenueId,
                flatFee.VenueTenantId, flatFee.ArtistTenantId, flatFee.DealType, flatFee.PaymentMethod, flatFee.StartDate,
                flatFee.EndDate, flatFee.Genres, flatFee.ArtistName, flatFee.VenueName, flatFee.TermsText,
                flatFee.PlatformTermsVersion, flatFee.ArtistSignature.ToValue(), flatFee.VenueSignature.ToValue(), flatFee.Fee),
            VenueHireAcceptedApplication venueHire => new StandardBookingAcceptance(
                venueHire.OperationId, venueHire.ApplicationId, venueHire.OpportunityId, venueHire.ArtistId, venueHire.VenueId,
                venueHire.VenueTenantId, venueHire.ArtistTenantId, venueHire.DealType, venueHire.PaymentMethod,
                venueHire.StartDate, venueHire.EndDate, venueHire.Genres, venueHire.ArtistName, venueHire.VenueName,
                venueHire.TermsText, venueHire.PlatformTermsVersion, venueHire.ArtistSignature.ToValue(),
                venueHire.VenueSignature.ToValue(), venueHire.HireFee),
            DoorSplitAcceptedApplication doorSplit => new DeferredBookingAcceptance(
                doorSplit.OperationId, doorSplit.ApplicationId, doorSplit.OpportunityId, doorSplit.ArtistId, doorSplit.VenueId,
                doorSplit.VenueTenantId, doorSplit.ArtistTenantId, doorSplit.DealType, doorSplit.PaymentMethod,
                doorSplit.StartDate, doorSplit.EndDate, doorSplit.Genres, doorSplit.ArtistName, doorSplit.VenueName,
                doorSplit.TermsText, doorSplit.PlatformTermsVersion, doorSplit.ArtistSignature.ToValue(),
                doorSplit.VenueSignature.ToValue(), doorSplit.ArtistDoorPercent, 0, doorSplit.PaymentMethodId),
            VersusAcceptedApplication versus => new DeferredBookingAcceptance(
                versus.OperationId, versus.ApplicationId, versus.OpportunityId, versus.ArtistId, versus.VenueId,
                versus.VenueTenantId, versus.ArtistTenantId, versus.DealType, versus.PaymentMethod, versus.StartDate,
                versus.EndDate, versus.Genres, versus.ArtistName, versus.VenueName, versus.TermsText,
                versus.PlatformTermsVersion, versus.ArtistSignature.ToValue(), versus.VenueSignature.ToValue(),
                versus.ArtistDoorPercent, versus.Guarantee, versus.PaymentMethodId),
            _ => throw new ArgumentOutOfRangeException(nameof(application), application, null)
        };
    }

    extension(SignatureDto signature)
    {
        private Signature ToValue() => new(
            signature.UserId,
            signature.AtUtc,
            signature.Ip,
            signature.UserAgent,
            signature.SignatoryName,
            signature.DrawnSignatureImage);
    }
}
