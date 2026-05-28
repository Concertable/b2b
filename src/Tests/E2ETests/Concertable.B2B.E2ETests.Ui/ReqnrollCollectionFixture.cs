using System.Threading.Tasks;
using Reqnroll;
using Xunit;

[CollectionDefinition("ReqnrollCollection", DisableParallelization = true)]
public class ReqnrollCollection : ICollectionFixture<ReqnrollCollectionFixture> { }

public class ReqnrollCollectionFixture : IAsyncLifetime
{
    public async Task InitializeAsync() =>
        await TestRunnerManager.OnTestRunStartAsync(GetType().Assembly);

    public async Task DisposeAsync() =>
        await TestRunnerManager.OnTestRunEndAsync(GetType().Assembly);
}
