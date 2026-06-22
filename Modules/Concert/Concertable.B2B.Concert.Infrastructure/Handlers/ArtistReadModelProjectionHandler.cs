using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Domain;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Handlers;

internal sealed class ArtistReadModelProjectionHandler : IIntegrationEventHandler<ArtistChangedEvent>
{
    private readonly ConcertDbContext context;

    public ArtistReadModelProjectionHandler(ConcertDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(ArtistChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(ArtistReadModelProjectionHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(ArtistReadModelProjectionHandler));

        var artist = await context.ArtistReadModels
            .Include(a => a.Genres)
            .FirstOrDefaultAsync(a => a.Id == e.ArtistId, ct);

        if (artist is null)
        {
            artist = new ArtistReadModel
            {
                Id = e.ArtistId,
                UserId = e.UserId,
                TenantId = e.TenantId,
                Name = e.Name,
                Avatar = e.Avatar,
                BannerUrl = e.BannerUrl,
                Address = new Address(e.County, e.Town),
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
            artist.TenantId = e.TenantId;
            artist.Name = e.Name;
            artist.Avatar = e.Avatar;
            artist.BannerUrl = e.BannerUrl;
            artist.Address = new Address(e.County, e.Town);
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
