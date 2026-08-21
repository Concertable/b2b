using Concertable.B2B.IntegrationTests.Fixtures;

namespace Concertable.B2B.Concert.IntegrationTests;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<ConcertApiFixture>;
