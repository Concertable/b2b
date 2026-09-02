using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Application.Contracts;

public abstract record AcceptedApplication(
    Guid OperationId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    DealType DealType,
    PaymentMethod PaymentMethod,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    string ArtistName,
    string VenueName,
    string TermsText,
    string PlatformTermsVersion,
    SignatureDto ArtistSignature,
    SignatureDto VenueSignature);

public sealed record FlatFeeAcceptedApplication(
    Guid OperationId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    PaymentMethod PaymentMethod,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    string ArtistName,
    string VenueName,
    string TermsText,
    string PlatformTermsVersion,
    SignatureDto ArtistSignature,
    SignatureDto VenueSignature,
    decimal Fee)
    : AcceptedApplication(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId,
        VenueTenantId, ArtistTenantId, DealType.FlatFee, PaymentMethod,
        StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature);

public sealed record DoorSplitAcceptedApplication(
    Guid OperationId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    PaymentMethod PaymentMethod,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    string ArtistName,
    string VenueName,
    string TermsText,
    string PlatformTermsVersion,
    SignatureDto ArtistSignature,
    SignatureDto VenueSignature,
    decimal ArtistDoorPercent,
    string PaymentMethodId,
    VerifyPayment? Verification)
    : AcceptedApplication(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId,
        VenueTenantId, ArtistTenantId, DealType.DoorSplit, PaymentMethod,
        StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature), IAcceptVerified;

public sealed record VersusAcceptedApplication(
    Guid OperationId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    PaymentMethod PaymentMethod,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    string ArtistName,
    string VenueName,
    string TermsText,
    string PlatformTermsVersion,
    SignatureDto ArtistSignature,
    SignatureDto VenueSignature,
    decimal Guarantee,
    decimal ArtistDoorPercent,
    string PaymentMethodId,
    VerifyPayment? Verification)
    : AcceptedApplication(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId,
        VenueTenantId, ArtistTenantId, DealType.Versus, PaymentMethod,
        StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature), IAcceptVerified;

public sealed record VenueHireAcceptedApplication(
    Guid OperationId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    PaymentMethod PaymentMethod,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    string ArtistName,
    string VenueName,
    string TermsText,
    string PlatformTermsVersion,
    SignatureDto ArtistSignature,
    SignatureDto VenueSignature,
    decimal HireFee,
    string PaymentMethodId)
    : AcceptedApplication(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId,
        VenueTenantId, ArtistTenantId, DealType.VenueHire, PaymentMethod,
        StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature);
