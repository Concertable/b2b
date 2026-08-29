using System.Globalization;
using System.Net;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Domain.ValueObjects;
using Concertable.B2B.Artist.Domain.Entities;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Venue.Domain.Entities;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class ApplicationFactory
{
    private const string SeedPaymentMethodId = "pm_card_visa";

    public static AcceptedApplication ToAcceptedApplication(
        ApplicationEntity application,
        ArtistEntity artist,
        VenueEntity venue,
        OpportunityEntity opportunity,
        DealEntity deal,
        DateTime acceptedAtUtc,
        Guid operationId)
    {
        application.BeginAcceptance(operationId);
        var venueSignature = new Signature(
            venue.UserId,
            acceptedAtUtc,
            IPAddress.Loopback,
            null,
            venue.Name,
            null);
        var genres = opportunity.Genres.ToList();
        var termsText = RenderTerms(deal);

        return deal switch
        {
            FlatFeeDealEntity flatFee => new FlatFeeAcceptedApplication(
                operationId, application.Id, opportunity.Id, artist.Id, venue.Id,
                opportunity.TenantId, artist.TenantId, deal.PaymentMethod,
                opportunity.Period.Start, opportunity.Period.End, genres,
                artist.Name, venue.Name, termsText, "2026-07",
                ToDto(application.ArtistESignature), ToDto(venueSignature), flatFee.Fee),
            DoorSplitDealEntity doorSplit => new DoorSplitAcceptedApplication(
                operationId, application.Id, opportunity.Id, artist.Id, venue.Id,
                opportunity.TenantId, artist.TenantId, deal.PaymentMethod,
                opportunity.Period.Start, opportunity.Period.End, genres,
                artist.Name, venue.Name, termsText, "2026-07",
                ToDto(application.ArtistESignature), ToDto(venueSignature), doorSplit.ArtistDoorPercent,
                SeedPaymentMethodId, ToContract(application.Verification)),
            VersusDealEntity versus => new VersusAcceptedApplication(
                operationId, application.Id, opportunity.Id, artist.Id, venue.Id,
                opportunity.TenantId, artist.TenantId, deal.PaymentMethod,
                opportunity.Period.Start, opportunity.Period.End, genres,
                artist.Name, venue.Name, termsText, "2026-07",
                ToDto(application.ArtistESignature), ToDto(venueSignature), versus.Guarantee,
                versus.ArtistDoorPercent, SeedPaymentMethodId,
                ToContract(application.Verification)),
            VenueHireDealEntity venueHire => new VenueHireAcceptedApplication(
                operationId, application.Id, opportunity.Id, artist.Id, venue.Id,
                opportunity.TenantId, artist.TenantId, deal.PaymentMethod,
                opportunity.Period.Start, opportunity.Period.End, genres,
                artist.Name, venue.Name, termsText, "2026-07",
                ToDto(application.ArtistESignature), ToDto(venueSignature), venueHire.HireFee,
                ((PrepaidApplication)application).PaymentMethodId),
            _ => throw new ArgumentOutOfRangeException(nameof(deal), deal, null)
        };
    }

    public static void FinishConstruction(
        ApplicationEntity application,
        ArtistEntity artist,
        OpportunityEntity opportunity,
        DealEntity deal,
        DateTime signedAtUtc)
    {
        application.With(nameof(ApplicationEntity.DealType), deal.DealType);
        application.With(nameof(ApplicationEntity.VenueTenantId), opportunity.TenantId);
        application.With(nameof(ApplicationEntity.ArtistTenantId), artist.TenantId);
        application.RecordArtistESignature(
            new Signature(artist.UserId, signedAtUtc, IPAddress.Loopback, null, artist.Name, null),
            ApplicationTermsFingerprint.Calculate(ToDto(deal), opportunity.Period));
    }

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

    private static TApplication InState<TApplication>(int artistId, int opportunityId, ApplicationState state)
        where TApplication : ApplicationEntity =>
        New<TApplication>()
            .With(nameof(ApplicationEntity.ArtistId), artistId)
            .With(nameof(ApplicationEntity.OpportunityId), opportunityId)
            .With(nameof(ApplicationEntity.State), state);

    private static DealDto ToDto(DealEntity deal) => deal switch
    {
        FlatFeeDealEntity flatFee => new FlatFeeDealDto
        {
            Id = flatFee.Id,
            PaymentMethod = flatFee.PaymentMethod,
            Fee = flatFee.Fee
        },
        DoorSplitDealEntity doorSplit => new DoorSplitDealDto
        {
            Id = doorSplit.Id,
            PaymentMethod = doorSplit.PaymentMethod,
            ArtistDoorPercent = doorSplit.ArtistDoorPercent
        },
        VersusDealEntity versus => new VersusDealDto
        {
            Id = versus.Id,
            PaymentMethod = versus.PaymentMethod,
            Guarantee = versus.Guarantee,
            ArtistDoorPercent = versus.ArtistDoorPercent
        },
        VenueHireDealEntity venueHire => new VenueHireDealDto
        {
            Id = venueHire.Id,
            PaymentMethod = venueHire.PaymentMethod,
            HireFee = venueHire.HireFee
        },
        _ => throw new ArgumentOutOfRangeException(nameof(deal), deal, null)
    };

    private static SignatureDto ToDto(Signature signature) =>
        new(
            signature.UserId,
            signature.AtUtc,
            signature.Ip,
            signature.UserAgent,
            signature.SignatoryName,
            signature.DrawnSignatureImage);

    private static VerifyPayment? ToContract(PaymentVerification? verification) => verification switch
    {
        SuccessfulPaymentVerification succeeded =>
            new VerifyPaymentSucceeded(succeeded.ApplicationId, succeeded.ProviderTransactionId),
        FailedPaymentVerification failed =>
            new VerifyPaymentFailed(
                failed.ApplicationId,
                failed.ProviderTransactionId,
                new VerifyPaymentError(failed.Failure.Code, failed.Failure.Message)),
        null => null,
        _ => throw new ArgumentOutOfRangeException(nameof(verification), verification, null)
    };

    private static string RenderTerms(DealEntity deal) => deal switch
    {
        FlatFeeDealEntity flatFee =>
            $"The venue pays the artist a flat fee of {Gbp(flatFee.Fee)}.",
        DoorSplitDealEntity doorSplit =>
            $"The artist receives {Percent(doorSplit.ArtistDoorPercent)} of door revenue.",
        VersusDealEntity versus =>
            $"The artist receives a guarantee of {Gbp(versus.Guarantee)} plus {Percent(versus.ArtistDoorPercent)} of door revenue.",
        VenueHireDealEntity venueHire =>
            $"The artist pays the venue a hire fee of {Gbp(venueHire.HireFee)}.",
        _ => throw new ArgumentOutOfRangeException(nameof(deal), deal, null)
    };

    private static string Gbp(decimal amount) =>
        amount.ToString("C", CultureInfo.GetCultureInfo("en-GB"));

    private static string Percent(decimal percent) =>
        $"{percent.ToString("0.##", CultureInfo.GetCultureInfo("en-GB"))}%";
}
