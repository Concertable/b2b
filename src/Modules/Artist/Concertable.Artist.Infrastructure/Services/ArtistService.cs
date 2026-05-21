using Concertable.Application.Interfaces.Geometry;
using Concertable.Artist.Application.Mappers;
using Concertable.Artist.Application.Requests;
using Concertable.Shared.Geocoding;
using Concertable.Shared.Imaging;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.Shared.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Artist.Infrastructure.Services;

internal class ArtistService : IArtistService
{
    private readonly IArtistRepository artistRepository;
    private readonly IImageService imageService;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly IGeocodingService geocodingService;
    private readonly IGeometryProvider geometryProvider;

    public ArtistService(
        IArtistRepository artistRepository,
        IImageService imageService,
        ICurrentUser currentUser,
        IUserModule userModule,
        IGeocodingService geocodingService,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.artistRepository = artistRepository;
        this.imageService = imageService;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.geocodingService = geocodingService;
        this.geometryProvider = geometryProvider;
    }

    public Task<ArtistDto?> GetDetailsForCurrentUserAsync() =>
        artistRepository.GetDtoByUserIdAsync(currentUser.GetId());

    public async Task<ArtistDto> GetDetailsByIdAsync(int id) =>
        await artistRepository.GetDtoByIdAsync(id)
            ?? throw new NotFoundException("Artist not found");

    public async Task<ArtistDto> CreateAsync(CreateArtistRequest request)
    {
        var user = await userModule.GetManagerByIdAsync(currentUser.GetId())
            ?? throw new ForbiddenException("Manager not found");

        var bannerUrl = await imageService.UploadAsync(request.Banner);
        var avatarUrl = await imageService.UploadAsync(request.Avatar);
        var locationDto = await geocodingService.GetLocationAsync(request.Latitude, request.Longitude);
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

        var createdArtist = await artistRepository.AddAsync(artist);
        await artistRepository.SaveChangesAsync();

        return createdArtist.ToDto();
    }

    public async Task<ArtistDto> UpdateAsync(int id, UpdateArtistRequest request)
    {
        var artist = await artistRepository.GetFullByIdAsync(id)
            ?? throw new NotFoundException("Artist not found");

        if (artist.UserId != currentUser.GetId())
            throw new ForbiddenException("You do not own this Artist");

        var bannerUrl = request.Banner is not null
            ? await imageService.ReplaceAsync(request.Banner.File, request.Banner.Url)
            : artist.BannerUrl;

        artist.Update(request.Name, request.About, bannerUrl, request.Genres);

        var locationDto = await geocodingService.GetLocationAsync(request.Latitude, request.Longitude);
        artist.UpdateLocation(
            geometryProvider.CreatePoint(request.Latitude, request.Longitude),
            new Address(locationDto.County, locationDto.Town));

        if (request.Avatar is not null)
            artist.UpdateAvatar(await imageService.ReplaceAsync(request.Avatar, artist.Avatar));

        await artistRepository.SaveChangesAsync();

        return artist.ToDto();
    }

    public async Task<int> GetIdForCurrentUserAsync()
    {
        int? id = await artistRepository.GetIdByUserIdAsync(currentUser.GetId());

        if (id is null)
            throw new ForbiddenException("You do not own an Artist");

        return id.Value;
    }

    public async Task<bool> OwnsArtistAsync(int artistId)
    {
        var id = await artistRepository.GetIdByUserIdAsync(currentUser.GetId());
        return id == artistId;
    }
}
