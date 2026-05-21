using System.ComponentModel.DataAnnotations.Schema;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Domain;

[Table("OpportunityGenres")]
[PrimaryKey(nameof(OpportunityId), nameof(Genre))]
public class OpportunityGenreEntity
{
    public int OpportunityId { get; set; }
    public Genre Genre { get; set; }
    public OpportunityEntity Opportunity { get; set; } = null!;
}
