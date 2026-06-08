using Xunit;

namespace Concertable.B2B.IntegrationTests.Fixtures;

[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<ApiFixture>;
