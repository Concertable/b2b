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
    public List<Genre> Genres { get; private set; } = [];
    public OpportunityState State { get; private set; } = OpportunityState.Open;

    public static OpportunityEntity Create(int venueId, DateRange period, int dealId, IEnumerable<Genre>? genres = null) =>
        new()
        {
            VenueId = venueId,
            Period = period,
            DealId = dealId,
            Genres = genres?.ToList() ?? []
        };

    public void Update(DateRange period, int dealId, IEnumerable<Genre> genres)
    {
        Period = period;
        DealId = dealId;
        Genres = genres.ToList();
    }

    public void MarkFilled() => State = OpportunityState.Filled;
    public void Withdraw() => State = OpportunityState.Withdrawn;

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
