using System.ComponentModel;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Domain.Errors;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.State;
using Concertable.B2B.DataAccess.Application;
using Concertable.Contracts;
using Concertable.Kernel;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// Represents a published concert within the B2B platform.
/// Holds denormalized <see cref="ArtistReadModel"/> and <see cref="VenueReadModel"/> references
/// so the Concert module can satisfy queries in a single DB context without crossing module boundaries.
/// </summary>
[DisplayName(DisplayNames.Concert)]
public sealed class ConcertEntity : IIdEntity, IHasName, IHasDateRange, IEventRaiser, IVenueArtistTenantScoped
{
    public int Id { get; private set; }
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public Guid OperationId { get; private set; }
    public int BookingId { get; private set; }
    public int ApplicationId { get; private set; }
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public int VenueId { get; private set; }
    public DealType DealType { get; private set; }
    public bool RequiresDoorRevenue { get; private set; }
    public ConcertState State { get; private set; } = ConcertState.Draft;
    public Guid? CancellationOperationId { get; private set; }
    public string? FinancialOperationReferenceId { get; private set; }
    public string? FinancialFailureCode { get; private set; }
    public string? FinancialFailureMessage { get; private set; }
    public decimal? Fee { get; private set; }
    public decimal? HireFee { get; private set; }
    public decimal? ArtistDoorPercent { get; private set; }
    public decimal? Guarantee { get; private set; }
    public string? SettlementPaymentMethodId { get; private set; }
    public string Name { get; private set; } = null!;
    public string About { get; private set; } = null!;
    public string? BannerUrl { get; private set; }
    public string? Avatar { get; private set; }
    public decimal Price { get; private set; }
    public int TotalTickets { get; private set; }
    public int TicketsSold { get; private set; }
    public decimal? DoorRevenue { get; private set; }
    public DateRange Period { get; private set; } = null!;
    public DateTime? DatePosted { get; private set; }
    public ArtistReadModel Artist { get; set; } = null!;
    public VenueReadModel Venue { get; set; } = null!;
    public List<Genre> Genres { get; private set; } = [];
    public ICollection<ConcertImageEntity> Images { get; private set; } = [];

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    private ConcertEntity() { }

    public static ConcertEntity CreateDraft(
        ConfirmedBooking booking,
        string name,
        string about,
        IEnumerable<Genre> genres)
    {
        ArgumentNullException.ThrowIfNull(booking);
        if (booking.VenueTenantId == Guid.Empty || booking.ArtistTenantId == Guid.Empty)
            throw new InvalidOperationException("A concert cannot inherit unresolved booking tenants.");

        var concert = new ConcertEntity
        {
            OperationId = booking.OperationId,
            BookingId = booking.BookingId,
            ApplicationId = booking.ApplicationId,
            OpportunityId = booking.OpportunityId,
            VenueTenantId = booking.VenueTenantId,
            ArtistTenantId = booking.ArtistTenantId,
            ArtistId = booking.ArtistId,
            VenueId = booking.VenueId,
            DealType = booking.DealType,
            RequiresDoorRevenue = booking.RequiresDoorRevenue,
            Period = new DateRange(booking.StartDate, booking.EndDate),
            Name = name,
            About = about,
            Genres = genres.ToList()
        };

        switch (booking.Terms)
        {
            case FlatFeeBookingTerms flatFee:
                concert.Fee = flatFee.Fee;
                break;
            case DoorSplitBookingTerms doorSplit:
                concert.ArtistDoorPercent = doorSplit.ArtistDoorPercent;
                concert.SettlementPaymentMethodId = doorSplit.PaymentMethodId;
                break;
            case VersusBookingTerms versus:
                concert.Guarantee = versus.Guarantee;
                concert.ArtistDoorPercent = versus.ArtistDoorPercent;
                concert.SettlementPaymentMethodId = versus.PaymentMethodId;
                break;
            case VenueHireBookingTerms venueHire:
                concert.HireFee = venueHire.HireFee;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(booking), booking.Terms, null);
        }

        return concert;
    }

    public void IncrementTicketsSold(int quantity) => TicketsSold += quantity;

    public UnitResult<DoorRevenueDeclarationError> DeclareDoorRevenue(decimal doorRevenue)
    {
        if (doorRevenue < 0)
            return new DoorRevenueDeclarationError.NegativeRevenue();
        DoorRevenue = doorRevenue;
        return new Success();
    }

    public void Update(string name, string about, decimal price, int totalTickets)
    {
        Name = name;
        About = about;
        Price = price;
        TotalTickets = totalTickets;
        events.Raise(new ConcertChangedDomainEvent(Id, totalTickets, price, Period, DatePosted));
    }

    public void Post(string name, string about, decimal price, int totalTickets, DateTime now)
    {
        if (State is not (ConcertState.Draft or ConcertState.Posted))
            throw new InvalidOperationException($"Concert {Id} cannot be posted from {State}.");

        Name = name;
        About = about;
        Price = price;
        TotalTickets = totalTickets;
        DatePosted = now;
        State = ConcertState.Posted;
        events.Raise(new ConcertChangedDomainEvent(Id, totalTickets, price, Period, now));
        events.Raise(new ConcertPostedDomainEvent(Id));
    }

    public Guid BeginCancellation()
    {
        if (State is not (ConcertState.Draft or ConcertState.Posted or ConcertState.CancellationFailed))
            throw new InvalidOperationException($"Concert {Id} cannot begin cancellation from {State}.");

        CancellationOperationId = Guid.NewGuid();
        State = ConcertState.CancellationPending;
        return CancellationOperationId.Value;
    }

    public void RecordCancellationFailure(string code, string message)
    {
        if (State != ConcertState.CancellationPending)
            throw new InvalidOperationException($"Concert {Id} cannot record cancellation failure from {State}.");

        State = ConcertState.CancellationFailed;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
    }

    public void Cancel()
    {
        if (State is not (ConcertState.CancellationPending or ConcertState.CancellationFailed))
            throw new InvalidOperationException($"Concert {Id} cannot cancel from {State}.");

        State = ConcertState.Cancelled;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
        events.Raise(new ConcertCancelledDomainEvent(Id));
    }

    public void BeginSettlement(string providerReferenceId)
    {
        if (State is not (ConcertState.Draft or ConcertState.Posted or ConcertState.SettlementFailed))
            throw new InvalidOperationException($"Concert {Id} cannot begin settlement from {State}.");

        State = ConcertState.AwaitingSettlement;
        FinancialOperationReferenceId = providerReferenceId;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
    }

    public void RecordSettlementFailure(string providerReferenceId, string code, string message)
    {
        EnsureSettlementReference(providerReferenceId);
        if (State != ConcertState.AwaitingSettlement)
            throw new InvalidOperationException($"Concert {Id} cannot record settlement failure from {State}.");

        State = ConcertState.SettlementFailed;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
    }

    public void CompleteSettlement(string? providerReferenceId = null)
    {
        if (providerReferenceId is not null)
            EnsureSettlementReference(providerReferenceId);
        if (State is not (ConcertState.Draft or ConcertState.Posted or ConcertState.AwaitingSettlement or ConcertState.SettlementFailed))
            throw new InvalidOperationException($"Concert {Id} cannot complete from {State}.");

        State = ConcertState.Complete;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
    }

    public decimal CalculateSettlementGross() => DealType switch
    {
        DealType.FlatFee => Fee!.Value,
        DealType.DoorSplit => TotalRevenue() * ArtistDoorPercent!.Value / 100m,
        DealType.Versus => Guarantee!.Value + TotalRevenue() * ArtistDoorPercent!.Value / 100m,
        DealType.VenueHire => HireFee!.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(DealType), DealType, null)
    };

    public Guid SettlementPayerTenantId =>
        DealType == DealType.VenueHire ? ArtistTenantId : VenueTenantId;

    public Guid SettlementPayeeTenantId =>
        DealType == DealType.VenueHire ? VenueTenantId : ArtistTenantId;

    private decimal TotalRevenue() =>
        TicketsSold * Price + DoorRevenue
        ?? throw new InvalidOperationException($"Concert {Id} has no declared door revenue.");

    private void EnsureSettlementReference(string providerReferenceId)
    {
        if (FinancialOperationReferenceId != providerReferenceId)
            throw new InvalidOperationException(
                $"Concert {Id} expects settlement {FinancialOperationReferenceId}, not {providerReferenceId}.");
    }
}
