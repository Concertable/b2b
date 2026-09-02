using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Financial;
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
    PaymentMethod PaymentMethod,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres,
    string ArtistName,
    string VenueName,
    string TermsText,
    string PlatformTermsVersion,
    Signature ArtistSignature,
    Signature VenueSignature)
{
    public abstract DealType DealType { get; }
    public abstract bool RequiresDoorRevenue { get; }
    internal abstract FinancialOperation ExpectedFinancialOperation { get; }

    internal abstract ContractEntity CreateContract(int bookingId, DateTime createdAtUtc);
}

internal sealed record FlatFeeAcceptance(
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
    Signature ArtistSignature,
    Signature VenueSignature,
    decimal Fee)
    : BookingAcceptance(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId, VenueTenantId, ArtistTenantId,
        PaymentMethod, StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature)
{
    public override DealType DealType => DealType.FlatFee;
    public override bool RequiresDoorRevenue => false;
    internal override FinancialOperation ExpectedFinancialOperation => FinancialOperation.CaptureEscrow;

    internal override ContractEntity CreateContract(int bookingId, DateTime createdAtUtc) =>
        FlatFeeContract.Create(bookingId, this, createdAtUtc);
}

internal sealed record VenueHireAcceptance(
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
    Signature ArtistSignature,
    Signature VenueSignature,
    decimal HireFee,
    string PaymentMethodId)
    : BookingAcceptance(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId, VenueTenantId, ArtistTenantId,
        PaymentMethod, StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature)
{
    public override DealType DealType => DealType.VenueHire;
    public override bool RequiresDoorRevenue => false;
    internal override FinancialOperation ExpectedFinancialOperation => FinancialOperation.DepositEscrow;

    internal override ContractEntity CreateContract(int bookingId, DateTime createdAtUtc) =>
        VenueHireContract.Create(bookingId, this, createdAtUtc);
}

internal sealed record DoorSplitAcceptance(
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
    Signature ArtistSignature,
    Signature VenueSignature,
    decimal ArtistDoorPercent,
    string PaymentMethodId)
    : BookingAcceptance(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId, VenueTenantId, ArtistTenantId,
        PaymentMethod, StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature)
{
    public override DealType DealType => DealType.DoorSplit;
    public override bool RequiresDoorRevenue => true;
    internal override FinancialOperation ExpectedFinancialOperation => FinancialOperation.VerifyPayment;

    internal override ContractEntity CreateContract(int bookingId, DateTime createdAtUtc) =>
        DoorSplitContract.Create(bookingId, this, createdAtUtc);
}

internal sealed record VersusAcceptance(
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
    Signature ArtistSignature,
    Signature VenueSignature,
    decimal Guarantee,
    decimal ArtistDoorPercent,
    string PaymentMethodId)
    : BookingAcceptance(
        OperationId, ApplicationId, OpportunityId, ArtistId, VenueId, VenueTenantId, ArtistTenantId,
        PaymentMethod, StartDate, EndDate, Genres, ArtistName, VenueName, TermsText, PlatformTermsVersion,
        ArtistSignature, VenueSignature)
{
    public override DealType DealType => DealType.Versus;
    public override bool RequiresDoorRevenue => true;
    internal override FinancialOperation ExpectedFinancialOperation => FinancialOperation.VerifyPayment;

    internal override ContractEntity CreateContract(int bookingId, DateTime createdAtUtc) =>
        VersusContract.Create(bookingId, this, createdAtUtc);
}

internal sealed record Signature(
    Guid UserId,
    DateTime AtUtc,
    System.Net.IPAddress Ip,
    string? UserAgent,
    string SignatoryName,
    string? DrawnSignatureImage);
