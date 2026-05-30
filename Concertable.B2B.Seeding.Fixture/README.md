# Concertable.B2B.Seeding.Fixture

Canonical wire-level data for B2B's published seed set — every `VenueChangedEvent`, `ArtistChangedEvent`, `ConcertChangedEvent` that B2B's seeders would publish, expressed as static `IReadOnlyList<>`s of event records.

Pure data, no behaviour. References only the three B2B `*.Contracts` projects and `Concertable.Seeding.Identity`. Zero runtime / EF / Domain dependencies — ships as a private NuGet package in the split-repo future.

## Who consumes it

- `Concertable.B2B.Seeding` — real B2B's seeders project from these lists into Domain entities + publish the events.
- `Concertable.B2B.Seeding.Simulator` — standalone Worker that publishes these events when real B2B isn't running (consumed by `Concertable.Customer.AppHost` for standalone dev).
- `Concertable.Customer.E2ETests` — test assertions against the canonical entity set.

The point: whichever path produced the events (real B2B or simulator), downstream projection state in Customer / Search is byte-identical.

## Full pattern

See [`../Concertable.B2B.Seeding.Simulator/CLAUDE.md`](../Concertable.B2B.Seeding.Simulator/CLAUDE.md) for the design principle, what NOT to do, how to add new entities, boundary checks, and cross-repo distribution.
