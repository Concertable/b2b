using Concertable.Shared;

namespace Concertable.Customer.Domain;

public class PreferenceEntity : IIdEntity
{
    private PreferenceEntity() { }

    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public double RadiusKm { get; private set; }
    public HashSet<GenrePreferenceEntity> GenrePreferences { get; private set; } = [];

    public static PreferenceEntity Create(Guid userId, double radiusKm, IEnumerable<Genre> genres)
    {
        var preference = new PreferenceEntity { UserId = userId, RadiusKm = radiusKm };
        preference.SyncGenres(genres);
        return preference;
    }

    public void Update(double radiusKm, IEnumerable<Genre> genres)
    {
        RadiusKm = radiusKm;
        SyncGenres(genres);
    }

    public void SyncGenres(IEnumerable<Genre> genres)
    {
        var target = genres.ToHashSet();
        GenrePreferences.RemoveWhere(gp => !target.Contains(gp.Genre));
        var existing = GenrePreferences.Select(gp => gp.Genre).ToHashSet();
        foreach (var g in target)
            if (!existing.Contains(g))
                GenrePreferences.Add(new GenrePreferenceEntity { Genre = g });
    }
}
