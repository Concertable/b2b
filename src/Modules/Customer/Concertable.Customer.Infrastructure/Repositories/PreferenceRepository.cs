using Concertable.Customer.Infrastructure.Data;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Customer.Infrastructure.Repositories;

internal class PreferenceRepository : Repository<PreferenceEntity>, IPreferenceRepository
{
    public PreferenceRepository(CustomerDbContext context) : base(context) { }

    public async new Task<IEnumerable<PreferenceEntity>> GetAllAsync()
    {
        return await context.Preferences
            .Include(p => p.GenrePreferences)
            .ToListAsync();
    }

    public async new Task<PreferenceEntity?> GetByIdAsync(int id)
    {
        return await context.Preferences
            .Include(p => p.GenrePreferences)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PreferenceEntity?> GetByUserIdAsync(Guid id)
    {
        return await context.Preferences
            .Include(p => p.GenrePreferences)
            .FirstOrDefaultAsync(p => p.UserId == id);
    }

    public async Task<IEnumerable<PreferenceEntity>> GetByMatchingGenresAsync(IEnumerable<Genre> genres)
    {
        var target = genres.ToArray();
        return await context.Preferences
            .Where(p => p.GenrePreferences.Any(gp => target.Contains(gp.Genre)))
            .ToListAsync();
    }
}
