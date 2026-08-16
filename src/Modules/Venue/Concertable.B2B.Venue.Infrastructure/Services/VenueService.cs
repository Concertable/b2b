using Concertable.B2B.Venue.Application.Errors;
using Concertable.B2B.Venue.Application.Requests;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
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

    public Task<Result<VenueDetails, VenueError>> GetDetailsByIdAsync(
        int id,
        CancellationToken ct = default) =>
        publicRepository.GetDetailsByIdAsync(id, ct)
            .ToOption()
            .OrFailure(() => (VenueError)new VenueError.NotFound(id));

    public async Task<Result<VenueDetails, CreateVenueError>> CreateForActiveTenantAsync(
        CreateVenueRequest request,
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new CreateVenueError.NoActiveTenant();

        if (await repository.ExistsByTenantIdAsync(tenantId, ct))
            return new CreateVenueError.ActiveTenantAlreadyHasVenue();

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
                        var createdVenue = await repository.AddAsync(venue, ct);
                        try
                        {
                            await repository.SaveChangesAsync(ct);
                        }
                        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
                        {
                            return new CreateVenueError.ActiveTenantAlreadyHasVenue();
                        }

                        var details = await publicRepository.GetDetailsByIdAsync(createdVenue.Id, ct)
                            ?? throw new InvalidOperationException(
                                $"Venue {createdVenue.Id} not found after creation.");
                        return Result.Success<VenueDetails, CreateVenueError>(details);
                    }, errors => new CreateVenueError.Invalid(errors));
            }, errors => new CreateVenueError.Invalid(errors));
    }

    public async Task<Result<VenueDetails, UpdateVenueError>> UpdateForActiveTenantAsync(
        UpdateVenueRequest request,
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new UpdateVenueError.ActiveTenantNotFound();

        var venue = await repository.GetByTenantIdAsync(tenantId, ct);
        if (venue is null)
            return new UpdateVenueError.ActiveTenantNotFound();

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

                        var details = await publicRepository.GetDetailsByIdAsync(venue.Id, ct)
                            ?? throw new InvalidOperationException(
                                $"Venue {venue.Id} not found after update.");
                        return Result.Success<VenueDetails, UpdateVenueError>(details);
                    }, errors => new UpdateVenueError.Invalid(errors));
            }, errors => new UpdateVenueError.Invalid(errors));
    }

    public async Task<Result<VenueDetails, VenueError>> GetDetailsForActiveTenantAsync(
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return new VenueError.ActiveTenantNotFound();

        return await repository.GetDetailsByTenantIdAsync(tenantId, ct)
            .ToOption()
            .OrFailure((VenueError)new VenueError.ActiveTenantNotFound());
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
        await publicRepository.GetSummaryAsync(id, ct);

    public async Task<Option<VenueOrgIdentity>> GetOrgIdentityByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        await publicRepository.GetOrgIdentityByTenantIdAsync(tenantId, ct);
}
