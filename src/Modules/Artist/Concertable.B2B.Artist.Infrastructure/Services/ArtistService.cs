using Concertable.B2B.Artist.Application.Requests;
using Concertable.B2B.Artist.Application.Errors;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.DataAccess.Infrastructure.Extensions;
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
    private readonly IArtistReadRepository readRepository;
    private readonly IImageService imageService;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly IGeocodingClient geocodingClient;
    private readonly IGeometryProvider geometryProvider;

    public ArtistService(
        IArtistRepository repository,
        IArtistReadRepository readRepository,
        IImageService imageService,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        IGeocodingClient geocodingClient,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.repository = repository;
        this.readRepository = readRepository;
        this.imageService = imageService;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.geocodingClient = geocodingClient;
        this.geometryProvider = geometryProvider;
    }

    public async Task<Option<ArtistDetails>> GetDetailsAsync(
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();

        return await repository.GetDetailsByTenantIdAsync(tenantId, ct);
    }

    public async Task<Option<ArtistDetails>> GetDetailsByIdAsync(
        int id,
        CancellationToken ct = default) =>
        await readRepository.GetDetailsByIdAsync(id, ct);

    public async Task<Result<ArtistDetails, CreateArtistError>> CreateAsync(
        CreateArtistRequest request,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();

        if (await repository.ExistsByTenantIdAsync(tenantId, ct))
            return new CreateArtistError.ArtistAlreadyExists();

        return await ArtistEntity.ValidateProfile(request.Name, request.About)
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
                    .BindAsync(async artist =>
                    {
                        if (!(await repository.TryInsertAsync(artist, ct))
                            .TryGetValue(out var createdArtist))
                            return new CreateArtistError.ArtistAlreadyExists();

                        var details = await readRepository.GetDetailsByIdAsync(createdArtist.Id, ct)
                            ?? throw new InvalidOperationException(
                                $"Artist {createdArtist.Id} not found after creation.");
                        return Result.Success<ArtistDetails, CreateArtistError>(details);
                    }, errors => new CreateArtistError.Invalid(errors));
            }, errors => new CreateArtistError.Invalid(errors));
    }

    public async Task<Result<ArtistDetails, UpdateArtistError>> UpdateAsync(
        UpdateArtistRequest request,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();

        var artist = await repository.GetByTenantIdAsync(tenantId, ct);
        if (artist is null)
            return new UpdateArtistError.ArtistNotFound();

        return await ArtistEntity.ValidateProfile(request.Name, request.About)
            .BindAsync(async () =>
            {
                var bannerUrl = request.Banner is not null
                    ? await imageService.ReplaceAsync(request.Banner, artist.BannerUrl)
                    : artist.BannerUrl;
                return await artist.Update(request.Name, request.About, bannerUrl, request.Genres)
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

                        await repository.SaveChangesAsync(ct);

                        var details = await readRepository.GetDetailsByIdAsync(artist.Id, ct)
                            ?? throw new InvalidOperationException(
                                $"Artist {artist.Id} not found after update.");
                        return Result.Success<ArtistDetails, UpdateArtistError>(details);
                    }, errors => new UpdateArtistError.Invalid(errors));
            }, errors => new UpdateArtistError.Invalid(errors));
    }

    public async Task<bool> OwnsArtistAsync(int artistId, CancellationToken ct = default) =>
        tenantContext.TenantId is { } tenantId
        && await repository.GetTenantIdByIdAsync(artistId, ct) == tenantId;

    public async Task<Option<ArtistSummary>> GetSummaryAsync(
        int id,
        CancellationToken ct = default) =>
        await readRepository.GetSummaryAsync(id, ct);

    public Task<IReadOnlySet<Genre>> GetGenresAsync(
        int id,
        CancellationToken ct = default) =>
        readRepository.GetGenresAsync(id, ct);

    public async Task<Option<TenantContact>> GetContactByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        (await readRepository.GetContactByTenantIdAsync(tenantId, ct)).ToOption();
}
