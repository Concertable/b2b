using Concertable.B2B.Artist.Application.Requests;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Services.Geometry;
using Concertable.Shared.Geocoding.Application;
using Concertable.Shared.Imaging.Application;

namespace Concertable.B2B.Artist.Infrastructure.Services;

internal sealed class ArtistService : IArtistService
{
    private readonly IArtistRepository repository;
    private readonly IImageService imageService;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly IGeocodingClient geocodingClient;
    private readonly IGeometryProvider geometryProvider;

    public ArtistService(
        IArtistRepository repository,
        IImageService imageService,
        ICurrentUser currentUser,
        IUserModule userModule,
        IGeocodingClient geocodingClient,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.repository = repository;
        this.imageService = imageService;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.geocodingClient = geocodingClient;
        this.geometryProvider = geometryProvider;
    }

    public Task<ArtistDetails?> GetDetailsForCurrentUserAsync() =>
        repository.GetDetailsByUserIdAsync(currentUser.GetId());

    public async Task<ArtistDetails> GetDetailsByIdAsync(int id) =>
        await repository.GetDetailsByIdAsync(id)
            ?? throw new NotFoundException("Artist not found");

    public async Task<ArtistDetails> CreateAsync(CreateArtistRequest request)
    {
        var user = await userModule.GetManagerByIdAsync(currentUser.GetId())
            ?? throw new ForbiddenException("Manager not found");

        var bannerUrl = await imageService.UploadAsync(request.Banner);
        var avatarUrl = await imageService.UploadAsync(request.Avatar);
        var locationDto = await geocodingClient.GetLocationAsync(request.Latitude, request.Longitude);
        var location = geometryProvider.CreatePoint(request.Latitude, request.Longitude);
        var address = new Address(locationDto.County, locationDto.Town);

        var artist = ArtistEntity.Create(
            user.Id,
            request.Name,
            request.About,
            bannerUrl,
            avatarUrl,
            location,
            address,
            user.Email,
            request.Genres);

        var createdArtist = await repository.AddAsync(artist);
        await repository.SaveChangesAsync();

        return await repository.GetDetailsByIdAsync(createdArtist.Id)
            ?? throw new InternalServerException($"Artist {createdArtist.Id} not found after creation.");
    }

    public async Task<ArtistDetails> UpdateAsync(int id, UpdateArtistRequest request)
    {
        var artist = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException("Artist not found");

        if (artist.UserId != currentUser.GetId())
            throw new ForbiddenException("You do not own this Artist");

        var bannerUrl = request.Banner is not null
            ? await imageService.ReplaceAsync(request.Banner.File, request.Banner.Url)
            : artist.BannerUrl;

        artist.Update(request.Name, request.About, bannerUrl, request.Genres);

        var locationDto = await geocodingClient.GetLocationAsync(request.Latitude, request.Longitude);
        artist.UpdateLocation(
            geometryProvider.CreatePoint(request.Latitude, request.Longitude),
            new Address(locationDto.County, locationDto.Town));

        if (request.Avatar is not null)
            artist.UpdateAvatar(await imageService.ReplaceAsync(request.Avatar, artist.Avatar));

        await repository.SaveChangesAsync();

        return await repository.GetDetailsByIdAsync(id)
            ?? throw new InternalServerException($"Artist {id} not found after update.");
    }

    public async Task<int> GetIdForCurrentUserAsync()
    {
        int? id = await repository.GetIdByUserIdAsync(currentUser.GetId());
        ForbiddenException.ThrowIfNull(id, "You do not own an Artist");

        return id.Value;
    }

    public async Task<bool> OwnsArtistAsync(int artistId)
    {
        var id = await repository.GetIdByUserIdAsync(currentUser.GetId());
        return id == artistId;
    }
}
