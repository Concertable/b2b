using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class OpportunityEntity : IIdEntity, IHasDateRange, IEquatable<OpportunityEntity>, ITenantScoped
{
    private OpportunityEntity() { }

    public int Id { get; private set; }
    public Guid TenantId { get; set; }
    public int VenueId { get; set; }
    public DateRange Period { get; private set; } = null!;
    public VenueReadModel Venue { get; set; } = null!;
    public int ContractId { get; private set; }
    public HashSet<ApplicationEntity> Applications { get; private set; } = [];
    public List<Genre> Genres { get; private set; } = [];

    public static OpportunityEntity Create(int venueId, DateRange period, int contractId, IEnumerable<Genre>? genres = null) =>
        new()
        {
            VenueId = venueId,
            Period = period,
            ContractId = contractId,
            Genres = genres?.ToList() ?? []
        };

    public void Update(DateRange period, int contractId, IEnumerable<Genre> genres)
    {
        Period = period;
        ContractId = contractId;
        Genres = genres.ToList();
    }

    public bool Equals(OpportunityEntity? other) => other is not null && Id == other.Id;

    public override bool Equals(object? obj) => Equals(obj as OpportunityEntity);

    public override int GetHashCode() => Id.GetHashCode();
}
