using System.ComponentModel;
using Concertable.B2B.Application.Contracts;
using AcceptedCommitment = Concertable.B2B.Application.Contracts.PaymentCommitment;
using PaymentCommitment = Concertable.B2B.Booking.Contracts.PaymentCommitment;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Domain.ValueObjects;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Booking.Domain.Entities;

[DisplayName(Booking.Contracts.DisplayNames.Contract)]
public abstract class ContractEntity : IIdEntity, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public int BookingId { get; private set; }
    public string VenueName { get; private set; } = null!;
    public string ArtistName { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;
    public DealType DealType { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string TermsText { get; private set; } = null!;
    public string PlatformTermsVersion { get; private set; } = null!;
    public string MandateTermsVersion { get; private set; } = null!;
    internal PaymentCommitment Commitment { get; private set; } = null!;
    internal Signature ArtistSignature { get; private set; } = null!;
    internal Signature VenueSignature { get; private set; } = null!;
    public string? PdfBlobName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    protected ContractEntity() { }

    private protected ContractEntity(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));

        var application = snapshot.Application;
        var opportunity = application.Opportunity;
        var contract = snapshot.Contract;
        BookingId = bookingId;
        VenueTenantId = opportunity.Venue.TenantId;
        ArtistTenantId = application.Artist.TenantId;
        VenueName = opportunity.Venue.Name;
        ArtistName = application.Artist.Name;
        Period = new DateRange(opportunity.StartDate, opportunity.EndDate);
        DealType = contract.Terms.DealType;
        PaymentMethod = contract.PaymentMethod;
        TermsText = contract.TermsText;
        PlatformTermsVersion = contract.PlatformTermsVersion;
        MandateTermsVersion = contract.MandateTermsVersion;
        Commitment = Commit(contract.Commitment);
        ArtistSignature = Sign(contract.ArtistSignature);
        VenueSignature = Sign(contract.VenueSignature);
        CreatedAtUtc = createdAtUtc;
        PdfBlobName = $"contracts/{bookingId}-{Guid.NewGuid():N}.pdf";
    }

    private static PaymentCommitment Commit(AcceptedCommitment commitment) =>
        new(commitment.OperationType, commitment.ConsumerCorrelation);

    private static Signature Sign(ContractSignature signature) =>
        new(
            signature.UserId,
            signature.AtUtc,
            signature.Ip,
            signature.UserAgent,
            signature.SignatoryName,
            signature.DrawnSignatureImage);

    public abstract DealTerms Terms { get; }

    internal abstract ConfirmedBookingTerms ConfirmedTerms { get; }

    internal abstract FinancialOperation ExpectedFinancialOperation { get; }
}

public sealed class FlatFeeContract : ContractEntity
{
    public decimal Fee { get; private set; }

    private FlatFeeContract() { }

    private FlatFeeContract(
        int bookingId, ApplicationAcceptanceSnapshot snapshot, FlatFeeTerms terms, DateTime createdAtUtc)
        : base(bookingId, snapshot, createdAtUtc)
    {
        Fee = terms.Fee;
    }

    internal static FlatFeeContract Create(
        int bookingId, ApplicationAcceptanceSnapshot snapshot, FlatFeeTerms terms, DateTime createdAtUtc) =>
        new(bookingId, snapshot, terms, createdAtUtc);

    public override DealTerms Terms => new FlatFeeTerms(Fee);

    internal override ConfirmedBookingTerms ConfirmedTerms =>
        new ConfirmedBookingTerms.FlatFee(Fee);

    internal override FinancialOperation ExpectedFinancialOperation =>
        FinancialOperation.CaptureEscrow;
}

public sealed class VenueHireContract : ContractEntity
{
    public decimal HireFee { get; private set; }

    private VenueHireContract() { }

    private VenueHireContract(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        VenueHireTerms terms,
        DateTime createdAtUtc)
        : base(bookingId, snapshot, createdAtUtc)
    {
        HireFee = terms.HireFee;
    }

    internal static VenueHireContract Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        VenueHireTerms terms,
        DateTime createdAtUtc) =>
        new(bookingId, snapshot, terms, createdAtUtc);

    public override DealTerms Terms => new VenueHireTerms(HireFee);

    internal override ConfirmedBookingTerms ConfirmedTerms =>
        new ConfirmedBookingTerms.VenueHire(HireFee);

    internal override FinancialOperation ExpectedFinancialOperation =>
        FinancialOperation.DepositEscrow;
}

public abstract class DoorRevenueContract : ContractEntity
{
    public decimal ArtistDoorPercent { get; private set; }

    protected DoorRevenueContract() { }

    private protected DoorRevenueContract(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        ISettledFromDoorRevenue terms,
        DateTime createdAtUtc)
        : base(bookingId, snapshot, createdAtUtc)
    {
        ArtistDoorPercent = terms.ArtistDoorPercent;
    }

    internal override FinancialOperation ExpectedFinancialOperation =>
        FinancialOperation.VerifyPayment;
}

public sealed class DoorSplitContract : DoorRevenueContract
{
    private DoorSplitContract() { }

    private DoorSplitContract(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        DoorSplitTerms terms,
        DateTime createdAtUtc)
        : base(bookingId, snapshot, terms, createdAtUtc) { }

    internal static DoorSplitContract Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        DoorSplitTerms terms,
        DateTime createdAtUtc) =>
        new(bookingId, snapshot, terms, createdAtUtc);

    public override DealTerms Terms => new DoorSplitTerms(ArtistDoorPercent);

    internal override ConfirmedBookingTerms ConfirmedTerms =>
        new ConfirmedBookingTerms.DoorSplit(ArtistDoorPercent);
}

public sealed class VersusContract : DoorRevenueContract
{
    public decimal Guarantee { get; private set; }

    private VersusContract() { }

    private VersusContract(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        VersusTerms terms,
        DateTime createdAtUtc)
        : base(bookingId, snapshot, terms, createdAtUtc)
    {
        Guarantee = terms.Guarantee;
    }

    internal static VersusContract Create(
        int bookingId,
        ApplicationAcceptanceSnapshot snapshot,
        VersusTerms terms,
        DateTime createdAtUtc) =>
        new(bookingId, snapshot, terms, createdAtUtc);

    public override DealTerms Terms => new VersusTerms(Guarantee, ArtistDoorPercent);

    internal override ConfirmedBookingTerms ConfirmedTerms =>
        new ConfirmedBookingTerms.Versus(Guarantee, ArtistDoorPercent);
}
