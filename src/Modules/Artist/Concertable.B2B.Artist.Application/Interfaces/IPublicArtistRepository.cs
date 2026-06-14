using Concertable.B2B.Artist.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

/// <summary>
/// The public marketplace surface over artists — any artist's summary/details/genres, read with the
/// "Tenant" filter lifted (the artist row is public). Owner/host reads live on <see cref="IArtistRepository"/>.
/// </summary>
internal interface IPublicArtistRepository
{
    Task<ArtistSummary?> GetSummaryAsync(int id);
    Task<ArtistDetails?> GetDetailsByIdAsync(int id);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id);
}
