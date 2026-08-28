using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Booking.Domain.ValueObjects;

internal abstract record BookingAcceptance(
    Guid OperationId,
    int ApplicationId,
    int OpportunityId,
    int ArtistId,
    int VenueId,
    Guid VenueTenantId,
    Guid ArtistTenantId,
    DealType DealType,
    bool RequiresDoorRevenue,
    PaymentMethod PaymentMethod,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    string ArtistName,
    string VenueName,
    string TermsText,
    string PlatformTermsVersion,
    Signature ArtistSignature,
    Signature VenueSignature);

internal sealed record StandardBookingAcceptance(
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
    Signature ArtistSignature,
    Signature VenueSignature,
    decimal Amount)
    : BookingAcceptance(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId, VenueTenantId, ArtistTenantId, DealType,
        false, PaymentMethod, StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature);

internal sealed record DeferredBookingAcceptance(
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
    Signature ArtistSignature,
    Signature VenueSignature,
    decimal ArtistDoorPercent,
    decimal Guarantee,
    string PaymentMethodId)
    : BookingAcceptance(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId, VenueTenantId, ArtistTenantId, DealType,
        true, PaymentMethod, StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature);

internal sealed record Signature(
    Guid UserId,
    DateTime AtUtc,
    System.Net.IPAddress Ip,
    string? UserAgent,
    string SignatoryName,
    string? DrawnSignatureImage);
