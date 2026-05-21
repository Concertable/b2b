using Concertable.Application.Interfaces.Geometry;
using Concertable.Shared;
using Concertable.Shared.Exceptions;

namespace Concertable.Customer.Infrastructure.Services;

internal class PreferenceService : IPreferenceService
{
    private readonly IPreferenceRepository preferenceRepository;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly IGeometryCalculator geometryCalculator;

    public PreferenceService(
        IPreferenceRepository preferenceRepository,
        ICurrentUser currentUser,
        IUserModule userModule,
        IGeometryCalculator geometryCalculator)
    {
        this.preferenceRepository = preferenceRepository;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.geometryCalculator = geometryCalculator;
    }

    public async Task<PreferenceDto> CreateAsync(CreatePreferenceRequest request, Guid? userId = null)
    {
        var resolvedUserId = userId ?? currentUser.GetId();
        var preference = PreferenceEntity.Create(resolvedUserId, request.RadiusKm, request.Genres);

        await preferenceRepository.AddAsync(preference);
        await preferenceRepository.SaveChangesAsync();

        return preference.ToDto();
    }

    public async Task<IEnumerable<PreferenceDto>> GetAsync()
    {
        var preferences = await preferenceRepository.GetAllAsync();
        return preferences.ToDtos();
    }

    public async Task<PreferenceDto?> GetByUserIdAsync(Guid userId)
    {
        var preference = await preferenceRepository.GetByUserIdAsync(userId);
        return preference?.ToDto();
    }

    public Task<PreferenceDto?> GetByUserAsync() => GetByUserIdAsync(currentUser.GetId());

    public async Task<PreferenceDto> UpdateAsync(PreferenceDto preferenceDto)
    {
        var preference = await preferenceRepository.GetByIdAsync(preferenceDto.Id)
            ?? throw new NotFoundException("Preference not found");

        if (currentUser.GetId() != preference.UserId)
            throw new UnauthorizedAccessException("You do not own this preference");

        preference.Update(preferenceDto.RadiusKm, preferenceDto.Genres);

        preferenceRepository.Update(preference);
        await preferenceRepository.SaveChangesAsync();

        var updated = await preferenceRepository.GetByIdAsync(preference.Id);
        return updated!.ToDto();
    }

    public async Task<IReadOnlyCollection<Guid>> GetUserIdsByLocationAndGenresAsync(
        double latitude,
        double longitude,
        IEnumerable<Genre> genres)
    {
        var preferences = (await preferenceRepository.GetByMatchingGenresAsync(genres)).ToList();
        if (preferences.Count == 0) return [];

        var users = await userModule.GetByIdsAsync(preferences.Select(p => p.UserId));
        var usersById = users.ToDictionary(u => u.Id);

        return preferences
            .Select(p => usersById.TryGetValue(p.UserId, out var u) ? (User: u, p.RadiusKm) : default)
            .Where(x => x.User?.Latitude is not null && x.User.Longitude is not null)
            .Where(x => geometryCalculator.IsWithinRadius(
                x.User!.Latitude!.Value,
                x.User.Longitude!.Value,
                latitude,
                longitude,
                (int)x.RadiusKm))
            .Select(x => x.User!.Id)
            .ToList();
    }
}
