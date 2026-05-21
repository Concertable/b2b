namespace Concertable.User.Application.Interfaces;

internal interface IUserPreferenceService
{
    Task<IEnumerable<Guid>> GetUserIdsByPreferencesAsync();
}
