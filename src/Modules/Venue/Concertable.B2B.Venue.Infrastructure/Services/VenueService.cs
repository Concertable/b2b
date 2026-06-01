using Concertable.Kernel.Exceptions;
using Concertable.B2B.Venue.Application.Mappers;
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
    private readonly IVenueRepository venueRepository;
    private readonly IImageService imageService;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly IGeocodingService geocodingService;
    private readonly IGeometryProvider geometryProvider;

    public VenueService(
        IVenueRepository venueRepository,
        IImageService imageService,
        ICurrentUser currentUser,
        IUserModule userModule,
        IGeocodingService geocodingService,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.venueRepository = venueRepository;
        this.imageService = imageService;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.geocodingService = geocodingService;
        this.geometryProvider = geometryProvider;
    }

    public async Task<VenueDto> GetDetailsByIdAsync(int id)
    {
        return await venueRepository.GetDtoByIdAsync(id)
            ?? throw new NotFoundException("Venue not found");
    }

    public async Task<VenueDto> CreateAsync(CreateVenueRequest request)
    {
        var user = await userModule.GetManagerByIdAsync(currentUser.GetId())
            ?? throw new ForbiddenException("Manager not found");

        var bannerUrl = await imageService.UploadAsync(request.Banner);
        var avatarUrl = await imageService.UploadAsync(request.Avatar);
        var locationDto = await geocodingService.GetLocationAsync(request.Latitude, request.Longitude);
        var location = geometryProvider.CreatePoint(request.Latitude, request.Longitude);
        var address = new Address(locationDto.County, locationDto.Town);

        var venue = VenueEntity.Create(
            user.Id,
            request.Name,
            request.About,
            bannerUrl,
            avatarUrl,
            location,
            address,
            user.Email);

        var createdVenue = await venueRepository.AddAsync(venue);
        await venueRepository.SaveChangesAsync();

        return createdVenue.ToDto();
    }

    public async Task<VenueDto> UpdateAsync(int id, UpdateVenueRequest request)
    {
        var venue = await venueRepository.GetFullByIdAsync(id)
            ?? throw new NotFoundException("Venue not found");

        if (venue.UserId != currentUser.GetId())
            throw new ForbiddenException("You do not own this venue");

        var bannerUrl = request.Banner is not null
            ? await imageService.ReplaceAsync(request.Banner.File, request.Banner.Url)
            : venue.BannerUrl;

        venue.Update(request.Name, request.About, bannerUrl);

        var locationDto = await geocodingService.GetLocationAsync(request.Latitude, request.Longitude);
        venue.UpdateLocation(
            geometryProvider.CreatePoint(request.Latitude, request.Longitude),
            new Address(locationDto.County, locationDto.Town));

        if (request.Avatar is not null)
            venue.UpdateAvatar(await imageService.ReplaceAsync(request.Avatar, venue.Avatar));

        await venueRepository.SaveChangesAsync();

        return venue.ToDto();
    }

    public Task<VenueDto?> GetDetailsForCurrentUserAsync() =>
        venueRepository.GetDtoByUserIdAsync(currentUser.GetId());

    public async Task<int> GetIdForCurrentUserAsync()
    {
        int? id = await venueRepository.GetIdByUserIdAsync(currentUser.GetId());
        ForbiddenException.ThrowIfNull(id, "You do not own a Venue");

        return id.Value;
    }

    public async Task<bool> OwnsVenueAsync(int venueId)
    {
        var id = await venueRepository.GetIdByUserIdAsync(currentUser.GetId());
        return id == venueId;
    }

    public async Task ApproveAsync(int id)
    {
        var venue = await venueRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Venue not found");

        venue.Approve();
        await venueRepository.SaveChangesAsync();
    }
}
