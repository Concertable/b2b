using Xunit;

namespace Concertable.B2B.IntegrationTests.Fixtures;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<ApiFixture>;
