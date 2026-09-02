using System.ComponentModel;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Domain.ValueObjects;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Booking.Domain.Entities;

[DisplayName(DisplayNames.Contract)]
public abstract class ContractEntity : IIdEntity, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public int BookingId { get; private set; }
    public int VenueId { get; private set; }
    public string VenueName { get; private set; } = null!;
    public int ArtistId { get; private set; }
    public string ArtistName { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;
    public DealType DealType { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string TermsText { get; private set; } = null!;
    public string PlatformTermsVersion { get; private set; } = null!;
    internal Signature ArtistSignature { get; private set; } = null!;
    internal Signature VenueSignature { get; private set; } = null!;
    public string? PdfBlobName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    protected ContractEntity() { }

    private protected ContractEntity(int bookingId, BookingAcceptance acceptance, DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(acceptance);
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));

        BookingId = bookingId;
        VenueTenantId = acceptance.VenueTenantId;
        ArtistTenantId = acceptance.ArtistTenantId;
        VenueId = acceptance.VenueId;
        VenueName = acceptance.VenueName;
        ArtistId = acceptance.ArtistId;
        ArtistName = acceptance.ArtistName;
        Period = new DateRange(acceptance.StartDate, acceptance.EndDate);
        DealType = acceptance.DealType;
        PaymentMethod = acceptance.PaymentMethod;
        TermsText = acceptance.TermsText;
        PlatformTermsVersion = acceptance.PlatformTermsVersion;
        ArtistSignature = acceptance.ArtistSignature;
        VenueSignature = acceptance.VenueSignature;
        CreatedAtUtc = createdAtUtc;
        PdfBlobName = $"contracts/{bookingId}-{Guid.NewGuid():N}.pdf";
    }

    public abstract DealTerms Terms { get; }
}

public sealed class FlatFeeContract : ContractEntity
{
    public decimal Fee { get; private set; }

    private FlatFeeContract() { }

    private FlatFeeContract(int bookingId, FlatFeeAcceptance acceptance, DateTime createdAtUtc)
        : base(bookingId, acceptance, createdAtUtc)
    {
        Fee = acceptance.Fee;
    }

    internal static FlatFeeContract Create(int bookingId, FlatFeeAcceptance acceptance, DateTime createdAtUtc) =>
        new(bookingId, acceptance, createdAtUtc);

    public override DealTerms Terms => new FlatFeeTerms(Fee);
}

public sealed class VenueHireContract : ContractEntity
{
    public decimal HireFee { get; private set; }
    public string PaymentMethodId { get; private set; } = null!;

    private VenueHireContract() { }

    private VenueHireContract(int bookingId, VenueHireAcceptance acceptance, DateTime createdAtUtc)
        : base(bookingId, acceptance, createdAtUtc)
    {
        HireFee = acceptance.HireFee;
        PaymentMethodId = acceptance.PaymentMethodId;
    }

    internal static VenueHireContract Create(int bookingId, VenueHireAcceptance acceptance, DateTime createdAtUtc) =>
        new(bookingId, acceptance, createdAtUtc);

    public override DealTerms Terms => new VenueHireTerms(HireFee);
}

public sealed class DoorSplitContract : ContractEntity
{
    public decimal ArtistDoorPercent { get; private set; }
    public string PaymentMethodId { get; private set; } = null!;

    private DoorSplitContract() { }

    private DoorSplitContract(int bookingId, DoorSplitAcceptance acceptance, DateTime createdAtUtc)
        : base(bookingId, acceptance, createdAtUtc)
    {
        ArtistDoorPercent = acceptance.ArtistDoorPercent;
        PaymentMethodId = acceptance.PaymentMethodId;
    }

    internal static DoorSplitContract Create(int bookingId, DoorSplitAcceptance acceptance, DateTime createdAtUtc) =>
        new(bookingId, acceptance, createdAtUtc);

    public override DealTerms Terms => new DoorSplitTerms(ArtistDoorPercent, PaymentMethodId);
}

public sealed class VersusContract : ContractEntity
{
    public decimal Guarantee { get; private set; }
    public decimal ArtistDoorPercent { get; private set; }
    public string PaymentMethodId { get; private set; } = null!;

    private VersusContract() { }

    private VersusContract(int bookingId, VersusAcceptance acceptance, DateTime createdAtUtc)
        : base(bookingId, acceptance, createdAtUtc)
    {
        Guarantee = acceptance.Guarantee;
        ArtistDoorPercent = acceptance.ArtistDoorPercent;
        PaymentMethodId = acceptance.PaymentMethodId;
    }

    internal static VersusContract Create(int bookingId, VersusAcceptance acceptance, DateTime createdAtUtc) =>
        new(bookingId, acceptance, createdAtUtc);

    public override DealTerms Terms => new VersusTerms(Guarantee, ArtistDoorPercent, PaymentMethodId);
}
