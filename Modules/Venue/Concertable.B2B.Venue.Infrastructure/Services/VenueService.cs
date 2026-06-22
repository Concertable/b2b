using Concertable.Kernel.Exceptions;
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

    public async Task<VenueDetails> GetDetailsByIdAsync(int id)
    {
        return await publicRepository.GetDetailsByIdAsync(id)
            ?? throw new NotFoundException("Venue not found");
    }

    public async Task<VenueDetails> CreateAsync(CreateVenueRequest request)
    {
        if (!tenantContext.HasTenant)
            throw new ForbiddenException("No active tenant");

        var bannerUrl = await imageService.UploadAsync(request.Banner);
        var avatarUrl = await imageService.UploadAsync(request.Avatar);
        var address = await geocodingClient.GetLocationAsync(request.Latitude, request.Longitude);
        var coordinates = geometryProvider.CreatePoint(request.Latitude, request.Longitude);

        var venue = VenueEntity.Create(
            currentUser.GetId(),
            request.Name,
            request.About,
            bannerUrl,
            avatarUrl,
            coordinates,
            address,
            currentUser.Email!);

        var createdVenue = await repository.AddAsync(venue);
        await repository.SaveChangesAsync();

        return await publicRepository.GetDetailsByIdAsync(createdVenue.Id)
            ?? throw new InternalServerException($"Venue {createdVenue.Id} not found after creation.");
    }

    public async Task<VenueDetails> UpdateAsync(int id, UpdateVenueRequest request)
    {
        var venue = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException("Venue not found");

        var bannerUrl = request.Banner is not null
            ? await imageService.ReplaceAsync(request.Banner, venue.BannerUrl)
            : venue.BannerUrl;

        venue.Update(request.Name, request.About, bannerUrl);

        var address = await geocodingClient.GetLocationAsync(request.Latitude, request.Longitude);
        venue.UpdateLocation(
            geometryProvider.CreatePoint(request.Latitude, request.Longitude),
            address);

        if (request.Avatar is not null)
            venue.UpdateAvatar(await imageService.ReplaceAsync(request.Avatar, venue.Avatar));

        await repository.SaveChangesAsync();

        return await publicRepository.GetDetailsByIdAsync(id)
            ?? throw new InternalServerException($"Venue {id} not found after update.");
    }

    public Task<VenueDetails?> GetDetailsForCurrentUserAsync() =>
        repository.GetDetailsForCurrentTenantAsync();

    public async Task<int> GetIdForCurrentUserAsync()
    {
        int? id = await repository.GetIdForCurrentTenantAsync();
        ForbiddenException.ThrowIfNull(id, "You do not own a Venue");

        return id.Value;
    }

    public async Task<bool> OwnsVenueAsync(int venueId)
    {
        var id = await repository.GetIdForCurrentTenantAsync();
        return id == venueId;
    }

    public async Task ApproveAsync(int id)
    {
        var venue = await adminRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Venue not found");

        venue.Approve();
        await adminRepository.SaveChangesAsync();
    }

    public async Task<VenueSummary> GetSummaryAsync(int id) =>
        await publicRepository.GetSummaryAsync(id)
            ?? throw new NotFoundException("Venue not found");
}
