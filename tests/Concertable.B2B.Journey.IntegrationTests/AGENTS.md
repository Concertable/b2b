# Concertable.B2B.Journey.IntegrationTests — cross-module journey integration tests

This suite owns complete multi-module B2B journeys. It drives the real host and observes each module
through HTTP or deliberate Contracts surfaces; it does not reference module Domain or Infrastructure
assemblies.

Conventions: the `dotnet-standards:integration-testing` skill, plus `dotnet:integration-testing` for this
system's fixture roster and shared harness.
