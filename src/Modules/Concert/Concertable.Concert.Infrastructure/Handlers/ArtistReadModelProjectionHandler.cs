using Concertable.Artist.Contracts.Events;
using Concertable.Concert.Domain;
using Concertable.Concert.Infrastructure.Data;
using Concertable.Messaging.Domain;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Handlers;

internal class ArtistReadModelProjectionHandler : IIntegrationEventHandler<ArtistChangedEvent>
{
    private readonly ConcertDbContext context;

    public ArtistReadModelProjectionHandler(ConcertDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(ArtistChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(ArtistReadModelProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(ArtistReadModelProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var artist = await context.ArtistReadModels
            .Include(a => a.Genres)
            .FirstOrDefaultAsync(a => a.Id == e.ArtistId, ct);

        if (artist is null)
        {
            artist = new ArtistReadModel
            {
                Id = e.ArtistId,
                UserId = e.UserId,
                Name = e.Name,
                Avatar = e.Avatar,
                BannerUrl = e.BannerUrl,
                County = e.County,
                Town = e.Town,
                Email = e.Email,
                Genres = e.Genres
                    .Select(g => new ArtistReadModelGenre { ArtistReadModelId = e.ArtistId, Genre = g })
                    .ToList()
            };
            context.ArtistReadModels.Add(artist);
        }
        else
        {
            artist.UserId = e.UserId;
            artist.Name = e.Name;
            artist.Avatar = e.Avatar;
            artist.BannerUrl = e.BannerUrl;
            artist.County = e.County;
            artist.Town = e.Town;
            artist.Email = e.Email;

            var desired = e.Genres.ToHashSet();
            var current = artist.Genres.Select(g => g.Genre).ToHashSet();

            foreach (var g in artist.Genres.Where(g => !desired.Contains(g.Genre)).ToList())
                artist.Genres.Remove(g);

            foreach (var g in desired.Where(g => !current.Contains(g)))
                artist.Genres.Add(new ArtistReadModelGenre { ArtistReadModelId = e.ArtistId, Genre = g });
        }

        await context.SaveChangesAsync(ct);
    }
}
