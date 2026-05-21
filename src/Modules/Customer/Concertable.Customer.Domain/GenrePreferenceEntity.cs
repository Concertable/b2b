using System.ComponentModel.DataAnnotations.Schema;
using Concertable.Shared;

namespace Concertable.Customer.Domain;

[Table("GenrePreferences")]
public class GenrePreferenceEntity : IIdEntity
{
    public int Id { get; private set; }
    public int PreferenceId { get; set; }
    public Genre Genre { get; set; }
    public PreferenceEntity Preference { get; set; } = null!;
}
