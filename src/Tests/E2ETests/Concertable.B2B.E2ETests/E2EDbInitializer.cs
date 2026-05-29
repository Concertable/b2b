using Concertable.DataAccess.Application;
using Concertable.E2ETests;
using B2BDevDbInitializer = Concertable.B2B.Web.DevDbInitializer;

namespace Concertable.B2B.E2ETests;

internal sealed class E2EDbInitializer : IDbInitializer
{
    private readonly B2BDevDbInitializer initializer;
    private readonly IEnumerable<IHealthWaiter> waiters;

    public E2EDbInitializer(B2BDevDbInitializer initializer, IEnumerable<IHealthWaiter> waiters)
    {
        this.initializer = initializer;
        this.waiters = waiters;
    }

    public async Task InitializeAsync()
    {
        await initializer.InitializeAsync();
        await Task.WhenAll(waiters.Select(w => w.WaitForReadyAsync(TimeSpan.FromMinutes(3))));
    }
}
