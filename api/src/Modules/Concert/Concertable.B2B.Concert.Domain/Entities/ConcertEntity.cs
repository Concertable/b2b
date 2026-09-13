using System.ComponentModel;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Domain.Errors;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.DataAccess.Application;
using Concertable.Contracts;
using Concertable.Kernel;
using Concertable.Payment.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Domain.Entities;

/// <summary>
/// Represents a published concert within the B2B platform.
/// Holds denormalized <see cref="ArtistReadModel"/> and <see cref="VenueReadModel"/> references
/// so the Concert module can satisfy queries in a single DB context without crossing module boundaries.
/// </summary>
[DisplayName(DisplayNames.Concert)]
public abstract class ConcertEntity : IIdEntity, IHasName, IHasDateRange, IConcurrencyVersioned, IEventRaiser, IVenueArtistTenantScoped
{
    private static readonly ConcertStateMachine stateMachine = new();

    public int Id { get; private set; }
    public byte[] Version { get; private set; } = null!;
    public Guid VenueTenantId { get; private set; }
    public Guid ArtistTenantId { get; private set; }
    public int BookingId { get; private set; }
    public int ApplicationId { get; private set; }
    public int OpportunityId { get; private set; }
    public int ArtistId { get; private set; }
    public int VenueId { get; private set; }
    public DealType DealType { get; private set; }
    public ConcertState State { get; private set; } = ConcertState.Draft;
    public Guid? CancellationOperationId { get; private set; }
    public Guid? SettlementOperationId { get; private set; }
    internal PaymentOperationReference SettlementPaymentReference { get; private set; }
    public decimal? SettlementGrossAmount { get; private set; }
    internal FinancialFailure? FinancialFailure { get; private set; }
    public string Name { get; private set; } = null!;
    public string About { get; private set; } = null!;
    public string? BannerUrl { get; private set; }
    public string? Avatar { get; private set; }
    public decimal Price { get; private set; }
    public int TotalTickets { get; private set; }
    public int TicketsSold { get; private set; }
    public DateRange Period { get; private set; } = null!;
    public DateTime? DatePosted { get; private set; }
    public ArtistReadModel Artist { get; set; } = null!;
    public VenueReadModel Venue { get; set; } = null!;
    public EfSet<Genre> Genres { get; private set; } = [];
    public ICollection<ConcertImageEntity> Images { get; private set; } = [];

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    protected ConcertEntity() { }

    public static ConcertEntity CreateDraft(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft) =>
        booking.Terms switch
        {
            ConfirmedBookingTerms.FlatFee terms =>
                FlatFeeConcert.Create(booking, draft, terms.Fee),
            ConfirmedBookingTerms.VenueHire terms =>
                VenueHireConcert.Create(booking, draft, terms.HireFee),
            ConfirmedBookingTerms.DoorSplit terms =>
                DoorSplitConcert.Create(
                    booking,
                    draft,
                    terms.ArtistDoorPercent),
            ConfirmedBookingTerms.Versus terms =>
                VersusConcert.Create(
                    booking,
                    draft,
                    terms.Guarantee,
                    terms.ArtistDoorPercent)
        };

    private protected ConcertEntity(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        DealType dealType)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(draft);
        if (booking.VenueTenantId == Guid.Empty || booking.ArtistTenantId == Guid.Empty)
            throw new InvalidOperationException("A concert cannot inherit unresolved booking tenants.");

        BookingId = booking.BookingId;
        ApplicationId = booking.ApplicationId;
        OpportunityId = booking.OpportunityId;
        VenueTenantId = booking.VenueTenantId;
        ArtistTenantId = booking.ArtistTenantId;
        ArtistId = booking.ArtistId;
        VenueId = booking.VenueId;
        DealType = dealType;
        Period = new DateRange(booking.StartDate, booking.EndDate);
        Name = draft.Name;
        About = draft.About;
        Genres = draft.Genres.ToEfSet();
        SettlementPaymentReference = booking.Commitment;
    }

    public void IncrementTicketsSold(int quantity) => TicketsSold += quantity;

    public void Update(string name, string about, decimal price, int totalTickets)
    {
        Name = name;
        About = about;
        Price = price;
        TotalTickets = totalTickets;
        events.Raise(new ConcertChangedDomainEvent(Id, totalTickets, price, Period, DatePosted));
    }

    public UnitResult<TransitionError<ConcertState, ConcertTrigger>> Post(string name, string about, decimal price, int totalTickets, DateTime now)
    {
        var transition = Fire(ConcertTrigger.Post);
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

    public Result<Guid, TransitionError<ConcertState, ConcertTrigger>> BeginCancellation()
    {
        var transition = Fire(ConcertTrigger.BeginCancellation);
        if (transition.TryGetError(out var error))
            return error;
        CancellationOperationId = Guid.CreateVersion7();
        return CancellationOperationId.Value;
    }

    internal UnitResult<TransitionError<ConcertState, ConcertTrigger>> ValidateBeginCancellation() =>
        Validate(ConcertTrigger.BeginCancellation);

    public UnitResult<TransitionError<ConcertState, ConcertTrigger>> RecordCancellationFailure(string code, string message)
    {
        var transition = Fire(ConcertTrigger.RecordCancellationFailure);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailure = new FinancialFailure(code, message);
        return new Success();
    }

    public UnitResult<TransitionError<ConcertState, ConcertTrigger>> Cancel()
    {
        var transition = Fire(ConcertTrigger.Cancel);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailure = null;
        events.Raise(new ConcertCancelledDomainEvent(Id));
        return new Success();
    }

    public Result<Guid, TransitionError<ConcertState, ConcertTrigger>> BeginSettlement()
    {
        var transition = Fire(ConcertTrigger.BeginSettlement);
        if (transition.TryGetError(out var error))
            return error;
        SettlementOperationId ??= Guid.CreateVersion7();
        SettlementGrossAmount ??= CalculateSettlementGross();
        FinancialFailure = null;
        return SettlementOperationId.Value;
    }

    internal void EnsureSettlementOperation(Guid operationId)
    {
        if (SettlementOperationId is null)
            throw new InvalidOperationException($"Concert {Id} has no settlement operation.");
        if (SettlementOperationId != operationId)
            throw new InvalidOperationException(
                $"Concert {Id} expects settlement operation {SettlementOperationId}, not {operationId}.");
    }

    public UnitResult<TransitionError<ConcertState, ConcertTrigger>> RecordSettlementFailure(string code, string message)
    {
        var transition = Fire(ConcertTrigger.RecordSettlementFailure);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailure = new FinancialFailure(code, message);
        return new Success();
    }

    public UnitResult<TransitionError<ConcertState, ConcertTrigger>> CompleteSettlement()
    {
        var transition = Fire(ConcertTrigger.CompleteSettlement);
        if (transition.TryGetError(out var error))
            return error;
        FinancialFailure = null;
        return new Success();
    }

    internal UnitResult<TransitionError<ConcertState, ConcertTrigger>> ValidateCompleteSettlement() =>
        Validate(ConcertTrigger.CompleteSettlement);

    public Money SettlementGross => Money.Gbp(SettlementGrossAmount
        ?? throw new InvalidOperationException($"Concert {Id} has no settlement gross."));

    public abstract decimal CalculateSettlementGross();

    public abstract Guid SettlementPayerTenantId { get; }

    public abstract Guid SettlementPayeeTenantId { get; }

    private UnitResult<TransitionError<ConcertState, ConcertTrigger>> Fire(ConcertTrigger trigger)
    {
        var transition = Transition(trigger);
        return transition.TryGetError(out var error) ? error : new Success();
    }

    private UnitResult<TransitionError<ConcertState, ConcertTrigger>> Validate(ConcertTrigger trigger)
    {
        var transition = stateMachine.Transition(State, trigger);
        return transition.TryGetError(out var error) ? error : new Success();
    }

    private Result<ConcertState, TransitionError<ConcertState, ConcertTrigger>> Transition(ConcertTrigger trigger)
    {
        var transition = stateMachine.Transition(State, trigger);
        if (transition.TryGetValue(out var next))
            State = next;
        return transition;
    }

}

public sealed class FlatFeeConcert : ConcertEntity
{
    public decimal Fee { get; private set; }

    private FlatFeeConcert() { }

    private FlatFeeConcert(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        decimal fee)
        : base(booking, draft, DealType.FlatFee)
    {
        Fee = fee;
    }

    internal static FlatFeeConcert Create(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        decimal fee) =>
        new(booking, draft, fee);

    public override decimal CalculateSettlementGross() => Fee;

    public override Guid SettlementPayerTenantId => VenueTenantId;

    public override Guid SettlementPayeeTenantId => ArtistTenantId;

}

public sealed class VenueHireConcert : ConcertEntity
{
    public decimal HireFee { get; private set; }

    private VenueHireConcert() { }

    private VenueHireConcert(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        decimal hireFee)
        : base(booking, draft, DealType.VenueHire)
    {
        HireFee = hireFee;
    }

    internal static VenueHireConcert Create(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        decimal hireFee) =>
        new(booking, draft, hireFee);

    public override decimal CalculateSettlementGross() => HireFee;

    public override Guid SettlementPayerTenantId => ArtistTenantId;

    public override Guid SettlementPayeeTenantId => VenueTenantId;

}

public abstract class DoorRevenueConcert : ConcertEntity
{
    public decimal ArtistDoorPercent { get; private set; }
    public decimal? DoorRevenue { get; private set; }

    protected DoorRevenueConcert() { }

    private protected DoorRevenueConcert(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        DealType dealType,
        decimal artistDoorPercent)
        : base(booking, draft, dealType)
    {
        ArtistDoorPercent = artistDoorPercent;
    }

    public UnitResult<DoorRevenueDeclarationError> DeclareDoorRevenue(decimal doorRevenue)
    {
        if (doorRevenue < 0)
            return new DoorRevenueDeclarationError.NegativeRevenue();
        DoorRevenue = doorRevenue;
        return new Success();
    }

    public override Guid SettlementPayerTenantId => VenueTenantId;

    public override Guid SettlementPayeeTenantId => ArtistTenantId;

    private protected decimal TotalRevenue() =>
        TicketsSold * Price + DoorRevenue
        ?? throw new InvalidOperationException($"Concert {Id} has no declared door revenue.");
}

public sealed class DoorSplitConcert : DoorRevenueConcert
{
    private DoorSplitConcert() { }

    private DoorSplitConcert(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        decimal artistDoorPercent)
        : base(
            booking,
            draft,
            DealType.DoorSplit,
            artistDoorPercent) { }

    internal static DoorSplitConcert Create(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        decimal artistDoorPercent) =>
        new(booking, draft, artistDoorPercent);

    public override decimal CalculateSettlementGross() =>
        TotalRevenue() * ArtistDoorPercent / 100m;
}

public sealed class VersusConcert : DoorRevenueConcert
{
    public decimal Guarantee { get; private set; }

    private VersusConcert() { }

    private VersusConcert(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        decimal guarantee,
        decimal artistDoorPercent)
        : base(
            booking,
            draft,
            DealType.Versus,
            artistDoorPercent)
    {
        Guarantee = guarantee;
    }

    internal static VersusConcert Create(
        ConfirmedBookingSnapshot booking,
        ConcertDraft draft,
        decimal guarantee,
        decimal artistDoorPercent) =>
        new(booking, draft, guarantee, artistDoorPercent);

    public override decimal CalculateSettlementGross() =>
        Guarantee + TotalRevenue() * ArtistDoorPercent / 100m;
}
