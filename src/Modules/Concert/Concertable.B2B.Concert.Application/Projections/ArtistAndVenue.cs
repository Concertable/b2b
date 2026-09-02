using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Application.Projections;

internal readonly record struct ArtistAndVenue(ArtistReadModel Artist, VenueReadModel Venue);
