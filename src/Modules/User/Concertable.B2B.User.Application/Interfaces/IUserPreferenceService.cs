namespace Concertable.B2B.User.Application.Interfaces;

internal interface IUserPreferenceService
{
    Task<IEnumerable<Guid>> GetUserIdsByPreferencesAsync();
}
