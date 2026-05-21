using Concertable.Shared;

namespace Concertable.Concert.Domain;

public class OpportunityEntity : IIdEntity, IHasDateRange, IEquatable<OpportunityEntity>
{
    private OpportunityEntity() { }

    public int Id { get; private set; }
    public int VenueId { get; set; }
    public DateRange Period { get; private set; } = null!;
    public VenueReadModel Venue { get; set; } = null!;
    public int ContractId { get; private set; }
    public HashSet<ApplicationEntity> Applications { get; private set; } = [];
    public HashSet<OpportunityGenreEntity> OpportunityGenres { get; private set; } = [];

    public static OpportunityEntity Create(int venueId, DateRange period, int contractId, IEnumerable<Genre>? genres = null)
    {
        var opportunity = new OpportunityEntity
        {
            VenueId = venueId,
            Period = period,
            ContractId = contractId
        };

        if (genres is not null)
            opportunity.SyncGenres(genres);

        return opportunity;
    }

    public void Update(DateRange period, int contractId, IEnumerable<Genre> genres)
    {
        Period = period;
        ContractId = contractId;
        SyncGenres(genres);
    }

    public void SyncGenres(IEnumerable<Genre> genres)
    {
        var target = genres.ToHashSet();
        OpportunityGenres.RemoveWhere(og => !target.Contains(og.Genre));
        var existing = OpportunityGenres.Select(og => og.Genre).ToHashSet();
        foreach (var g in target)
            if (!existing.Contains(g))
                OpportunityGenres.Add(new OpportunityGenreEntity { Genre = g });
    }

    public bool Equals(OpportunityEntity? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => Equals(obj as OpportunityEntity);

    public override int GetHashCode() => Id.GetHashCode();
}
