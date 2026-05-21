using Concertable.Customer.Application.DTOs;
using Concertable.Customer.Application.Requests;
using Concertable.Shared;

namespace Concertable.Customer.Application.Interfaces;

internal interface IPreferenceService
{
    Task<PreferenceDto?> GetByUserIdAsync(Guid userId);
    Task<PreferenceDto?> GetByUserAsync();
    Task<IEnumerable<PreferenceDto>> GetAsync();
    Task<PreferenceDto> CreateAsync(CreatePreferenceRequest request, Guid? userId = null);
    Task<PreferenceDto> UpdateAsync(PreferenceDto preferenceDto);
    Task<IReadOnlyCollection<Guid>> GetUserIdsByLocationAndGenresAsync(double latitude, double longitude, IEnumerable<Genre> genres);
}
