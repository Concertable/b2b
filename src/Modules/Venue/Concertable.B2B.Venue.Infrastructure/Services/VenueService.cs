using Concertable.B2B.Venue.Application.Errors;
using Concertable.B2B.Venue.Application.Requests;
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
    private readonly IPublicVenueRepository publicRepository;
    private readonly IAdminVenueRepository adminRepository;
    private readonly IImageService imageService;
    private readonly ICurrentUser currentUser;
    private readonly ITenantContext tenantContext;
    private readonly IGeocodingClient geocodingClient;
    private readonly IGeometryProvider geometryProvider;

    public VenueService(
        IVenueRepository repository,
        IPublicVenueRepository publicRepository,
        IAdminVenueRepository adminRepository,
        IImageService imageService,
        ICurrentUser currentUser,
        ITenantContext tenantContext,
        IGeocodingClient geocodingClient,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.repository = repository;
        this.publicRepository = publicRepository;
        this.adminRepository = adminRepository;
        this.imageService = imageService;
        this.currentUser = currentUser;
        this.tenantContext = tenantContext;
        this.geocodingClient = geocodingClient;
        this.geometryProvider = geometryProvider;
    }

    public Task<Result<VenueDetails, VenueError>> GetDetailsByIdAsync(int id) =>
        publicRepository.GetDetailsByIdAsync(id)
            .ToOption()
            .OrFailure(() => (VenueError)new VenueError.NotFound(id));

    public async Task<Result<VenueDetails, CreateVenueError>> CreateAsync(CreateVenueRequest request)
    {
        if (!tenantContext.HasTenant)
            return new CreateVenueError.NoActiveTenant();

        return await VenueEntity.ValidateProfile(request.Name, request.About)
            .MapError(errors => (CreateVenueError)new CreateVenueError.Invalid(errors))
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
                    .MapError(errors => (CreateVenueError)new CreateVenueError.Invalid(errors))
                    .BindAsync(async venue =>
                    {
                        var createdVenue = await repository.AddAsync(venue);
                        await repository.SaveChangesAsync();

                        var details = await publicRepository.GetDetailsByIdAsync(createdVenue.Id)
                            ?? throw new InvalidOperationException(
                                $"Venue {createdVenue.Id} not found after creation.");
                        return Result.Success<VenueDetails, CreateVenueError>(details);
                    });
            });
    }

    public async Task<Result<VenueDetails, UpdateVenueError>> UpdateAsync(int id, UpdateVenueRequest request)
    {
        var venue = await repository.GetByIdAsync(id);
        if (venue is null)
            return new UpdateVenueError.VenueNotFound(id);

        return await VenueEntity.ValidateProfile(request.Name, request.About)
            .MapError(errors => (UpdateVenueError)new UpdateVenueError.Invalid(errors))
            .BindAsync(async () =>
            {
                var bannerUrl = request.Banner is not null
                    ? await imageService.ReplaceAsync(request.Banner, venue.BannerUrl)
                    : venue.BannerUrl;
                return await venue.Update(request.Name, request.About, bannerUrl)
                    .MapError(errors => (UpdateVenueError)new UpdateVenueError.Invalid(errors))
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

                        await repository.SaveChangesAsync();

                        var details = await publicRepository.GetDetailsByIdAsync(id)
                            ?? throw new InvalidOperationException($"Venue {id} not found after update.");
                        return Result.Success<VenueDetails, UpdateVenueError>(details);
                    });
            });
    }

    public Task<Result<VenueDetails, VenueError>> GetDetailsForCurrentUserAsync() =>
        repository.GetDetailsForCurrentTenantAsync()
            .ToOption()
            .OrFailure((VenueError)new VenueError.CurrentTenantNotFound());

    public async Task<Option<int>> GetIdForCurrentTenantAsync() =>
        (await repository.GetIdForCurrentTenantAsync()).ToOption();

    public async Task<bool> OwnsVenueAsync(int venueId)
    {
        var id = await repository.GetIdForCurrentTenantAsync();
        return id == venueId;
    }

    public async Task<UnitResult<ApproveVenueError>> ApproveAsync(int id)
    {
        var venue = await adminRepository.GetByIdAsync(id);
        if (venue is null)
            return new ApproveVenueError.VenueNotFound(id);

        venue.Approve();
        await adminRepository.SaveChangesAsync();
        return new Success();
    }

    public async Task<Option<VenueSummary>> GetSummaryAsync(int id) =>
        await publicRepository.GetSummaryAsync(id);

    public async Task<Option<VenueOrgIdentity>> GetOrgIdentityByTenantIdAsync(Guid tenantId) =>
        await publicRepository.GetOrgIdentityByTenantIdAsync(tenantId);
}
