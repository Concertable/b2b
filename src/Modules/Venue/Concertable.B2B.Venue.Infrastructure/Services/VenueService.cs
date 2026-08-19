using Concertable.B2B.Venue.Application.Errors;
using Concertable.B2B.Venue.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.DataAccess.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Services.Geometry;
using Concertable.Shared.Geocoding.Application;
using Concertable.Shared.Imaging.Application;

namespace Concertable.B2B.Venue.Infrastructure.Services;

internal sealed class VenueService : IVenueService
{
    private readonly IVenueRepository repository;
    private readonly IVenueReadRepository readRepository;
    private readonly IVenuePrivilegedRepository adminRepository;
    private readonly IImageService imageService;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly IGeocodingClient geocodingClient;
    private readonly IGeometryProvider geometryProvider;

    public VenueService(
        IVenueRepository repository,
        IVenueReadRepository readRepository,
        IVenuePrivilegedRepository adminRepository,
        IImageService imageService,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        IGeocodingClient geocodingClient,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.repository = repository;
        this.readRepository = readRepository;
        this.adminRepository = adminRepository;
        this.imageService = imageService;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.geocodingClient = geocodingClient;
        this.geometryProvider = geometryProvider;
    }

    public async Task<Option<VenueDetails>> GetDetailsByIdAsync(
        int id,
        CancellationToken ct = default) =>
        await readRepository.GetDetailsByIdAsync(id, ct);

    public async Task<Result<VenueDetails, CreateVenueError>> CreateAsync(
        CreateVenueRequest request,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();

        if (await repository.ExistsByTenantIdAsync(tenantId, ct))
            return new CreateVenueError.VenueAlreadyExists();

        return await VenueEntity.ValidateProfile(request.Name, request.About)
            .BindAsync(async () =>
            {
                var bannerUrl = await imageService.UploadAsync(request.Banner);
                var avatarUrl = await imageService.UploadAsync(request.Avatar);
                var address = await geocodingClient.GetLocationAsync(request.Latitude, request.Longitude);
                var coordinates = geometryProvider.CreatePoint(request.Latitude, request.Longitude);

                return await VenueEntity.Create(
                    currentUser.GetId(),
                    request.Name,
                    request.About,
                    bannerUrl,
                    avatarUrl,
                    coordinates,
                    address,
                    currentUser.Email!)
                    .BindAsync(async venue =>
                    {
                        if (!(await repository.TryInsertAsync(venue, ct))
                            .TryGetValue(out var createdVenue))
                            return new CreateVenueError.VenueAlreadyExists();

                        var details = await readRepository.GetDetailsByIdAsync(createdVenue.Id, ct)
                            ?? throw new InvalidOperationException(
                                $"Venue {createdVenue.Id} not found after creation.");
                        return Result.Success<VenueDetails, CreateVenueError>(details);
                    }, errors => new CreateVenueError.Invalid(errors));
            }, errors => new CreateVenueError.Invalid(errors));
    }

    public async Task<Result<VenueDetails, UpdateVenueError>> UpdateAsync(
        UpdateVenueRequest request,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();

        var venue = await repository.GetByTenantIdAsync(tenantId, ct);
        if (venue is null)
            return new UpdateVenueError.VenueNotFound();

        return await VenueEntity.ValidateProfile(request.Name, request.About)
            .BindAsync(async () =>
            {
                var bannerUrl = request.Banner is not null
                    ? await imageService.ReplaceAsync(request.Banner, venue.BannerUrl)
                    : venue.BannerUrl;
                return await venue.Update(request.Name, request.About, bannerUrl)
                    .BindAsync(async () =>
                    {
                        var address = await geocodingClient.GetLocationAsync(
                            request.Latitude,
                            request.Longitude);
                        venue.UpdateLocation(
                            geometryProvider.CreatePoint(request.Latitude, request.Longitude),
                            address);

                        if (request.Avatar is not null)
                            venue.UpdateAvatar(await imageService.ReplaceAsync(request.Avatar, venue.Avatar));

                        await repository.SaveChangesAsync(ct);

                        var details = await readRepository.GetDetailsByIdAsync(venue.Id, ct)
                            ?? throw new InvalidOperationException(
                                $"Venue {venue.Id} not found after update.");
                        return Result.Success<VenueDetails, UpdateVenueError>(details);
                    }, errors => new UpdateVenueError.Invalid(errors));
            }, errors => new UpdateVenueError.Invalid(errors));
    }

    public async Task<Option<VenueDetails>> GetDetailsAsync(
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();

        return await repository.GetDetailsByTenantIdAsync(tenantId, ct);
    }

    public async Task<bool> OwnsVenueAsync(int venueId, CancellationToken ct = default) =>
        tenantContext.TenantId is { } tenantId
        && await repository.GetTenantIdByIdAsync(venueId, ct) == tenantId;

    public async Task<UnitResult<ApproveVenueError>> ApproveAsync(
        int id,
        CancellationToken ct = default)
    {
        var venue = await adminRepository.GetByIdAsync(id, ct);
        if (venue is null)
            return new ApproveVenueError.VenueNotFound(id);

        venue.Approve();
        await adminRepository.SaveChangesAsync(ct);
        return new Success();
    }

    public async Task<Option<VenueSummary>> GetSummaryAsync(
        int id,
        CancellationToken ct = default) =>
        await readRepository.GetSummaryAsync(id, ct);

    public async Task<Option<int>> GetCurrentIdAsync(CancellationToken ct = default)
    {
        var venue = await repository.GetByTenantIdAsync(tenantContext.GetTenantId(), ct);
        return venue is null ? Option.None<int>() : Option.Some(venue.Id);
    }

    public async Task<Option<VenueProfile>> GetProfileAsync(
        int id,
        CancellationToken ct = default) =>
        (await readRepository.GetProfileAsync(id, ct)).ToOption();

    public async Task<Option<VenueProfile>> GetCurrentProfileAsync(CancellationToken ct = default) =>
        tenantContext.TenantId is { } tenantId
            ? (await readRepository.GetProfileByTenantIdAsync(tenantId, ct)).ToOption()
            : Option.None<VenueProfile>();
}
