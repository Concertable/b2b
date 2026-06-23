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
    private readonly IPublicArtistRepository publicRepository;
    private readonly IImageService imageService;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly IGeocodingClient geocodingClient;
    private readonly IGeometryProvider geometryProvider;

    public ArtistService(
        IArtistRepository repository,
        IPublicArtistRepository publicRepository,
        IImageService imageService,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        IGeocodingClient geocodingClient,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.repository = repository;
        this.publicRepository = publicRepository;
        this.imageService = imageService;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.geocodingClient = geocodingClient;
        this.geometryProvider = geometryProvider;
    }

    public Task<ArtistDetails?> GetDetailsForCurrentUserAsync() =>
        repository.GetDetailsForCurrentTenantAsync();

    public async Task<ArtistDetails> GetDetailsByIdAsync(int id) =>
        await publicRepository.GetDetailsByIdAsync(id)
            ?? throw new NotFoundException("Artist not found");

    public async Task<ArtistDetails> CreateAsync(CreateArtistRequest request)
    {
        if (!tenantContext.HasTenant)
            throw new ForbiddenException("No active tenant");

        var bannerUrl = await imageService.UploadAsync(request.Banner);
        var avatarUrl = await imageService.UploadAsync(request.Avatar);
        var address = await geocodingClient.GetLocationAsync(request.Latitude, request.Longitude);
        var coordinates = geometryProvider.CreatePoint(request.Latitude, request.Longitude);

        var artist = ArtistEntity.Create(
            currentUser.GetId(),
            request.Name,
            request.About,
            bannerUrl,
            avatarUrl,
            coordinates,
            address,
            currentUser.Email!,
            request.Genres);

        var createdArtist = await repository.AddAsync(artist);
        await repository.SaveChangesAsync();

        return await publicRepository.GetDetailsByIdAsync(createdArtist.Id)
            ?? throw new InternalServerException($"Artist {createdArtist.Id} not found after creation.");
    }

    public async Task<ArtistDetails> UpdateAsync(int id, UpdateArtistRequest request)
    {
        var artist = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException("Artist not found");

        var bannerUrl = request.Banner is not null
            ? await imageService.ReplaceAsync(request.Banner, artist.BannerUrl)
            : artist.BannerUrl;

        artist.Update(request.Name, request.About, bannerUrl, request.Genres);

        var address = await geocodingClient.GetLocationAsync(request.Latitude, request.Longitude);
        artist.UpdateLocation(
            geometryProvider.CreatePoint(request.Latitude, request.Longitude),
            address);

        if (request.Avatar is not null)
            artist.UpdateAvatar(await imageService.ReplaceAsync(request.Avatar, artist.Avatar));

        await repository.SaveChangesAsync();

        return await publicRepository.GetDetailsByIdAsync(id)
            ?? throw new InternalServerException($"Artist {id} not found after update.");
    }

    public async Task<int> GetIdForCurrentUserAsync()
    {
        int? id = await repository.GetIdForCurrentTenantAsync();
        ForbiddenException.ThrowIfNull(id, "You do not own an Artist");

        return id.Value;
    }

    public async Task<bool> OwnsArtistAsync(int artistId)
    {
        var id = await repository.GetIdForCurrentTenantAsync();
        return id == artistId;
    }

    public async Task<ArtistSummary> GetSummaryAsync(int id) =>
        await publicRepository.GetSummaryAsync(id)
            ?? throw new NotFoundException("Artist not found");

    public Task<IReadOnlySet<Genre>> GetGenresAsync(int id) =>
        publicRepository.GetGenresAsync(id);
}
