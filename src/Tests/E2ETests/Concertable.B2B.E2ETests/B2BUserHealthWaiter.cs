using Concertable.B2B.User.Domain;
using Concertable.B2B.User.Infrastructure.Data;
using Concertable.E2ETests;
using Concertable.Seeding.Identity;

namespace Concertable.B2B.E2ETests;

internal sealed class B2BUserHealthWaiter : IHealthWaiter
{
    private readonly UserDbContext context;
    private readonly DbHealthWaiter waiter;

    public B2BUserHealthWaiter(UserDbContext context, DbHealthWaiter waiter)
    {
        this.context = context;
        this.waiter = waiter;
    }

    public Task WaitForReadyAsync(TimeSpan timeout) =>
        waiter.WaitForCountAsync(context.Users, SeedUsers.TotalCount, timeout);
}
