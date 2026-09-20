using System.ComponentModel;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Opportunity.Domain.Entities;

[DisplayName(DisplayNames.Opportunity)]
public sealed class OpportunityEntity : IIdEntity, IHasDateRange, IEquatable<OpportunityEntity>, ITenantScoped
{
    private OpportunityEntity() { }

    public int Id { get; private set; }
    public Guid TenantId { get; set; }
    public int VenueId { get; set; }
    public DateRange Period { get; private set; } = null!;
    public int DealId { get; private set; }
    public EfSet<Genre> Genres { get; private set; } = [];
    public OpportunityState State { get; private set; } = OpportunityState.Open;
    public int? FilledByApplicationId { get; private set; }
    public List<int> CancelledApplicationIds { get; private set; } = [];

    public static OpportunityEntity Create(
        int venueId,
        DateRange period,
        int dealId,
        IReadOnlySet<Genre> genres) =>
        new()
        {
            VenueId = venueId,
            Period = period,
            DealId = dealId,
            Genres = genres.ToEfSet()
        };

    public void Update(DateRange period, int dealId, IReadOnlySet<Genre> genres)
    {
        Period = period;
        DealId = dealId;
        Genres = genres.ToEfSet();
    }

    public void MarkFilled(int applicationId)
    {
        if (State == OpportunityState.Open && !CancelledApplicationIds.Contains(applicationId))
        {
            State = OpportunityState.Filled;
            FilledByApplicationId = applicationId;
        }
    }
    public void Withdraw()
    {
        State = OpportunityState.Withdrawn;
        FilledByApplicationId = null;
    }
    public void CancelApplication(int applicationId)
    {
        if (!CancelledApplicationIds.Contains(applicationId))
            CancelledApplicationIds.Add(applicationId);
        if (State == OpportunityState.Filled && FilledByApplicationId == applicationId)
        {
            State = OpportunityState.Open;
            FilledByApplicationId = null;
        }
    }

    public bool Equals(OpportunityEntity? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => Equals(obj as OpportunityEntity);

    public override int GetHashCode() => Id.GetHashCode();
}

public enum OpportunityState
{
    Open,
    Filled,
    Withdrawn
}
