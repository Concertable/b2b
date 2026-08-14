using Concertable.B2B.Artist.Application.Requests;
using Concertable.B2B.Artist.Application.Errors;
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

    public Task<Result<ArtistDetails, ArtistError>> GetDetailsForCurrentUserAsync() =>
        repository.GetDetailsForCurrentTenantAsync()
            .ToOption()
            .OrFailure((ArtistError)new ArtistError.CurrentTenantNotFound());

    public Task<Result<ArtistDetails, ArtistError>> GetDetailsByIdAsync(int id) =>
        publicRepository.GetDetailsByIdAsync(id)
            .ToOption()
            .OrFailure(() => (ArtistError)new ArtistError.NotFound(id));

    public async Task<Result<ArtistDetails, CreateArtistError>> CreateAsync(CreateArtistRequest request)
    {
        if (!tenantContext.HasTenant)
            return new CreateArtistError.Forbidden();

        return await ArtistEntity.ValidateProfile(request.Name, request.About)
            .MapError(errors => (CreateArtistError)new CreateArtistError.Invalid(errors))
            .BindAsync(async () =>
            {
                var bannerUrl = await imageService.UploadAsync(request.Banner);
                var avatarUrl = await imageService.UploadAsync(request.Avatar);
                var address = await geocodingClient.GetLocationAsync(request.Latitude, request.Longitude);
                var coordinates = geometryProvider.CreatePoint(request.Latitude, request.Longitude);

                return await ArtistEntity.Create(
                    currentUser.GetId(),
                    request.Name,
                    request.About,
                    bannerUrl,
                    avatarUrl,
                    coordinates,
                    address,
                    currentUser.Email!,
                    request.Genres)
                    .MapError(errors => (CreateArtistError)new CreateArtistError.Invalid(errors))
                    .BindAsync(async artist =>
                    {
                        var createdArtist = await repository.AddAsync(artist);
                        await repository.SaveChangesAsync();

                        var details = await publicRepository.GetDetailsByIdAsync(createdArtist.Id)
                            ?? throw new InvalidOperationException(
                                $"Artist {createdArtist.Id} not found after creation.");
                        return Result.Success<ArtistDetails, CreateArtistError>(details);
                    });
            });
    }

    public async Task<Result<ArtistDetails, UpdateArtistError>> UpdateAsync(int id, UpdateArtistRequest request)
    {
        var artist = await repository.GetByIdAsync(id);
        if (artist is null)
            return new UpdateArtistError.NotFound(id);

        return await ArtistEntity.ValidateProfile(request.Name, request.About)
            .MapError(errors => (UpdateArtistError)new UpdateArtistError.Invalid(errors))
            .BindAsync(async () =>
            {
                var bannerUrl = request.Banner is not null
                    ? await imageService.ReplaceAsync(request.Banner, artist.BannerUrl)
                    : artist.BannerUrl;
                return await artist.Update(request.Name, request.About, bannerUrl, request.Genres)
                    .MapError(errors => (UpdateArtistError)new UpdateArtistError.Invalid(errors))
                    .BindAsync(async () =>
                    {
                        var address = await geocodingClient.GetLocationAsync(
                            request.Latitude,
                            request.Longitude);
                        artist.UpdateLocation(
                            geometryProvider.CreatePoint(request.Latitude, request.Longitude),
                            address);

                        if (request.Avatar is not null)
                            artist.UpdateAvatar(await imageService.ReplaceAsync(request.Avatar, artist.Avatar));

                        await repository.SaveChangesAsync();

                        var details = await publicRepository.GetDetailsByIdAsync(id)
                            ?? throw new InvalidOperationException($"Artist {id} not found after update.");
                        return Result.Success<ArtistDetails, UpdateArtistError>(details);
                    });
            });
    }

    public async Task<Option<int>> GetIdForCurrentUserAsync() =>
        (await repository.GetIdForCurrentTenantAsync()).ToOption();

    public async Task<bool> OwnsArtistAsync(int artistId)
    {
        var id = await repository.GetIdForCurrentTenantAsync();
        return id == artistId;
    }

    public async Task<Option<ArtistSummary>> GetSummaryAsync(int id) =>
        await publicRepository.GetSummaryAsync(id);

    public Task<IReadOnlySet<Genre>> GetGenresAsync(int id) =>
        publicRepository.GetGenresAsync(id);

    public async Task<Option<ArtistOrgIdentity>> GetOrgIdentityByTenantIdAsync(Guid tenantId) =>
        await publicRepository.GetOrgIdentityByTenantIdAsync(tenantId);
}
