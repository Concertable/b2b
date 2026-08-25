using System.ComponentModel;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Domain.Errors;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Lifecycle;
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
    private static readonly StateMachine stateMachine = new();

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
    public State State { get; private set; } = State.Draft;
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

    public UnitResult<TransitionError<State, Trigger>> Post(string name, string about, decimal price, int totalTickets, DateTime now)
    {
        var transition = Apply(Trigger.Post);
        if (transition.TryGetError(out var error))
            return error;
        Name = name;
        About = about;
        Price = price;
        TotalTickets = totalTickets;
        DatePosted = now;
        events.Raise(new ConcertChangedDomainEvent(Id, totalTickets, price, Period, now));
        events.Raise(new ConcertPostedDomainEvent(Id));
        return new Success();
    }

    public Result<Guid, TransitionError<State, Trigger>> BeginCancellation()
    {
        var transition = Apply(Trigger.BeginCancellation);
        if (transition.TryGetError(out var error))
            return error;
        CancellationOperationId = Guid.NewGuid();
        return CancellationOperationId.Value;
    }

    internal UnitResult<TransitionError<State, Trigger>> ValidateBeginCancellation() =>
        Validate(Trigger.BeginCancellation);

    public UnitResult<TransitionError<State, Trigger>> RecordCancellationFailure(string code, string message)
    {
        var transition = Apply(Trigger.RecordCancellationFailure);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
        return new Success();
    }

    public UnitResult<TransitionError<State, Trigger>> Cancel()
    {
        var transition = Apply(Trigger.Cancel);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
        events.Raise(new ConcertCancelledDomainEvent(Id));
        return new Success();
    }

    public UnitResult<TransitionError<State, Trigger>> BeginSettlement(string providerReferenceId)
    {
        var transition = Apply(Trigger.BeginSettlement);
        if (transition.TryGetError(out var error))
            return error;
        FinancialOperationReferenceId = providerReferenceId;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
        return new Success();
    }

    public UnitResult<TransitionError<State, Trigger>> RecordSettlementFailure(string providerReferenceId, string code, string message)
    {
        EnsureSettlementReference(providerReferenceId);
        var transition = Apply(Trigger.RecordSettlementFailure);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailureCode = code;
        FinancialFailureMessage = message;
        return new Success();
    }

    public UnitResult<TransitionError<State, Trigger>> CompleteSettlement(string? providerReferenceId = null)
    {
        if (providerReferenceId is not null)
            EnsureSettlementReference(providerReferenceId);
        var transition = Apply(Trigger.CompleteSettlement);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailureCode = null;
        FinancialFailureMessage = null;
        return new Success();
    }

    internal UnitResult<TransitionError<State, Trigger>> ValidateCompleteSettlement() =>
        Validate(Trigger.CompleteSettlement);

    private UnitResult<TransitionError<State, Trigger>> Apply(Trigger trigger)
    {
        var transition = Transition(trigger);
        return transition.TryGetError(out var error) ? error : new Success();
    }

    private UnitResult<TransitionError<State, Trigger>> Validate(Trigger trigger)
    {
        var transition = stateMachine.Transition(State, trigger);
        return transition.TryGetError(out var error) ? error : new Success();
    }

    private Result<State, TransitionError<State, Trigger>> Transition(Trigger trigger)
    {
        var transition = stateMachine.Transition(State, trigger);
        if (transition.TryGetValue(out var next))
            State = next;
        return transition;
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
