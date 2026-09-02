using Concertable.B2B.Concert.Application.Projections;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Specifications;

internal sealed class ApplicationSpecification : SpecificationBuilder<ApplicationEntity>
{
    public static ISpecification<ApplicationEntity> CreateWithArtistGenresAndVenue() =>
        new ApplicationSpecification()
            .Include(application => application.Artist.Genres)
            .Include(application => application.Opportunity.Venue);

    public static ISpecification<ApplicationEntity, ArtistAndVenue?> CreateArtistAndVenue() =>
        new ApplicationSpecification()
            .Select(application => new ArtistAndVenue(application.Artist, application.Opportunity.Venue));
}
