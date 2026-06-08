# Concertable.B2B.Seed.Simulator

## What it is

A Worker host that publishes B2B's full seed event set (every `VenueChangedEvent`, `ArtistChangedEvent`, `ConcertChangedEvent` B2B's own seed would produce — ~117 events total) on startup, then exits cleanly. It's an Aspire resource registered **only** in standalone-consumer AppHosts that need B2B-published projection data when real B2B isn't running.

Today the only consumer is `Concertable.Customer.AppHost`. Any future standalone host that depends on B2B's projection data (a standalone Search.AppHost, for example) would register the same simulator.

The simulator is **not** registered in the umbrella `Concertable.AppHost`. Real B2B runs in the umbrella and publishes these events for real; the simulator would double-publish.

## Why it exists

Concertable is a multi-microservice system. Customer (and other downstream services) reference B2B only via `Concertable.B2B.X.Contracts` projects — the integration event records and DTO shapes. They never reference B2B's runtime code. See [`api/ARCHITECTURE.md`](../../../ARCHITECTURE.md) for the full statement of the microservice premise.

This means `Concertable.Customer.AppHost` runs standalone with Auth + Customer.Web + Search + Payment + SPAs but **without B2B**. With no B2B running, Customer's projection event handlers receive nothing. Customer's projection tables (`[concert].[Concerts]`, `[venue].[Venues]`, `[artist].[Artists]`) stay empty. The Customer SPA shows nothing. Dev experience is unusable.

The seeding convention (`api/docs/SEEDING_CONVENTIONS.md`) forbids the obvious hack — direct `context.XReadModels.AddRange(...)` — because read-models in production are written only by event handlers, never directly. Bypassing that flow in seeders would mean the seeding code path differs from production, which is exactly the bug-multiplier the convention exists to prevent.

The simulator is the convention-compliant answer. It runs as a Worker, publishes the same events real B2B would publish, exits. Customer's projection handlers do their normal work and populate the tables. Identical code path to production.

## Design principle: tight sync with B2B's own seed

The simulator and B2B's own `Concertable.B2B.Seed.Infrastructure.SeedState` **both derive from the same source** — `Concertable.B2B.Seed.Contracts`. The fixture exposes `VenueSeedSpec` / `ArtistSeedSpec` / `ConcertSeedSpec` records that convert in two directions: `ToChangedEvent()` (used by the simulator and by real B2B's outbox path) and the static factories `VenueFactory` / `ArtistFactory` / `ConcertFactory` (used by B2B's `SeedState` to build the matching Domain entities). Same source, two derivations — Customer's projection of venue id 1 is byte-identical to whatever B2B writes for venue id 1 when its own seeders run.

This is not just "the IDs match." Every field on the event — name, about, avatar, banner, county, town, latitude, longitude, email, user id, genres, etc. — comes from the spec. Customer's projection ends up with the same rows in standalone mode and umbrella mode.

Tight sync matters because Customer's behavior must not depend on which AppHost is running. A test that asserts on the seed concert's venue email passes either way. A developer manually browsing the Customer SPA sees the same data either way. No "works only under umbrella" surprises.

The other ~95 entities in B2B's SeedState (Applications, Bookings, Contracts, Opportunities, Transactions) are B2B-internal — they don't publish events Customer or Search consume, so they're not in the fixture and not sync-relevant. Customer sees their projected effects only through events the fixture covers.

## The fixture: `Concertable.B2B.Seed.Contracts`

Path: `api/Concertable.B2B/Concertable.B2B.Seed.Contracts/SeedCatalog.cs` (shell — ctor, `Now`) plus three partial files: `SeedCatalog.Venues.cs`, `SeedCatalog.Artists.cs`, `SeedCatalog.Concerts.cs`.

The fixture is a DI singleton — `services.AddSingleton<SeedCatalog>();` plus `services.AddSingleton(TimeProvider.System);`. The ctor captures `now` once via the injected `TimeProvider`; every property access serves the same coherent snapshot.

Holds:

- `IReadOnlyList<VenueSeedSpec> Venues` — 35 explicit literals, one per seed venue.
- `IReadOnlyList<ArtistSeedSpec> Artists` — 35 explicit literals, one per seed artist.
- `IReadOnlyList<ConcertSeedSpec> Concerts` — lazy-cached list of 47 entries (time-relative `Period` / `DatePosted` derive from the captured `now`).
- `DateTime Now` — the `now` snapshot captured at construction. Consumers needing a coherent time within the fixture's scope read from here.

Each `XSeedSpec` carries the same fields as its matching `XChangedEvent` and converts via the extension method `ToChangedEvent()` (in `Concertable.B2B.Seed.Contracts/SeedSpecMappers.cs`). The matching B2B-side construction lives in static factories under `Concertable.B2B.Seed.Infrastructure/Factories/` (`VenueFactory` / `ArtistFactory` / `ConcertFactory`) so Domain stays out of Contracts.

Does **not** hold:

- B2B Domain entities (`VenueEntity`, `ArtistEntity`, etc.) — those are B2B-internal. The fixture is a contract-level project that ships across service boundaries; importing Domain types would break the boundary.
- B2B-internal entities that don't publish events Customer/Search consume — Applications, Bookings, Contracts, Opportunities, Transactions. Those stay in `Concertable.B2B.Seed.Infrastructure.SeedState` and exist only when real B2B runs.
- Label-style constants. The fixture is 100% data. Tests that need "the upcoming concert" filter by `Name` (e.g. `fixture.Concerts.First(c => c.Name == "Upcoming FlatFee Show")`) — the label lives in the test, not the fixture.
- Compression scaffolding — no shared `Locations[]` array with index-cycling, no `XRow`/`OppSpec` row schemas, no magic `HashSet<int>` index sets. Each spec literal carries its own county/town/lat/long/email values explicitly. For concerts, the only construction sugar is two helpers (`Concert(...)` / `ConcertHire(...)`) that take the spec's own fields verbatim — they don't introduce a row schema or hidden indexing.

Project deps:

- `Concertable.B2B.Venue.Contracts`
- `Concertable.B2B.Artist.Contracts`
- `Concertable.B2B.Concert.Contracts`
- `Concertable.Seed.Identity` (for `SeedUsers.VenueManagerId(n)` / `VenueManagerEmail(n)` / `ArtistManagerId(n)` / `ArtistManagerEmail(n)` — the shared Guid generators for seed user identities)

Nothing else. The fixture is a tiny, NuGet-shaped project — it can ship as a private package in the split-repo world.

## How to add a new entity

To add another seed venue/artist/concert visible to Customer in both standalone and umbrella:

1. Add the new `XSeedSpec` literal to the appropriate partial (`SeedCatalog.Venues.cs` / `Artists.cs` / `Concerts.cs`). Write every field explicitly. Use `SeedUsers.X(n)` for the user-derived IDs/emails. For concerts pick `Concert(...)` for venue-paid or `ConcertHire(...)` for artist-paid (VenueHire) — the helpers take the same args, only the resolved `PayeeUserId` differs.

2. Confirm `Concertable.B2B.Seed.Infrastructure/SeedState.cs` projects from the catalog via `.Select(s => XFactory.Create(...))` (concert projection passes `bookingId`). If the projection is already there, no edit needed — the new entry comes through automatically.

3. Restart `Concertable.Customer.AppHost`. The simulator publishes the new event on startup; Customer's projection handler upserts; the SPA sees the new entity.

If the wire event gains a new field, update the matching `XSeedSpec` (in `Concertable.B2B.Seed.Contracts/Specs/`) and `ToChangedEvent()` in `Concertable.B2B.Seed.Contracts/SeedSpecMappers.cs`. The factory in `api/Concertable.B2B/Concertable.B2B.Seed.Infrastructure/Factories/XFactory.cs` only needs an edit if the entity's own `Create` takes the new field.

## What NOT to do

The failure modes I've personally hit during the design of this system:

- **Don't add `IDevSeeder`s that write to projection tables.** Anything like `services.AddScoped<IDevSeeder, ConcertProjectionDevSeeder>()` writing `context.Concerts.AddRange(...)` is a direct violation of `SEEDING_CONVENTIONS.md`. The convention is non-negotiable: read-models are written only by handlers.

- **Don't make the simulator depend on `Concertable.B2B.Seed.Infrastructure`.** That project owns B2B's Domain entities (`VenueEntity`, etc.) and transitively pulls B2B Domain into the simulator's reachable graph. The simulator must stay contract-only so it can ship as a container that consumers don't need B2B's source to build against.

- **Don't make `Concertable.Customer.Seed` know B2B-owned IDs.** Constants like `UpcomingConcertId = 13` previously lived in Customer.Seeding. Customer doesn't own those identifiers, B2B does — and they should not live as labels in the fixture either. Anything that needs "the upcoming concert" filters by `Name` on the injected `SeedCatalog.Concerts` list. The fixture stays 100% data.

- **Don't add the simulator to `Concertable.AppHost`.** Real B2B is already running in the umbrella; the simulator there would double-publish (consumers are idempotent so it wouldn't corrupt data, but it's wasted work and confusing in logs).

- **Don't run real B2B inside `Concertable.Customer.AppHost` to "solve" the empty-projection problem.** That re-monoliths the system. Customer's standalone AppHost exists precisely so Customer can be developed without B2B's runtime. If you find yourself adding `builder.AddProject<Projects.Concertable_B2B_Web>(...)` to Customer.AppHost, stop and re-read `api/ARCHITECTURE.md`.

- **Don't seed projection tables from inside the E2E `AppFixture`.** The old `ProjectionSeeder.cs` published events from inside the test fixture's seed host. That worked but it was a test-only hack — dev wasn't covered, the fixture had to register an ASB transport just for seeding, and it lived in the wrong layer. The simulator replaces that hack at the AppHost level so dev gets the same flow.

- **Don't put `XReadModel` factories or Domain mapper logic on the fixture.** The fixture holds the canonical `XSeedSpec` records plus their `ToChangedEvent()` instance method, full stop. Mapping spec → Domain entity is the responsibility of `Concertable.B2B.Seed.Infrastructure`'s static factories (`VenueFactory`/`ArtistFactory`/`ConcertFactory`) — these live in B2B-internal code because they reference `VenueEntity`/`ArtistEntity`/`ConcertEntity`, which Customer must not see. Mapping `XChangedEvent` → read-model entity is the responsibility of Customer's `XProjectionHandler`s.

## Cross-repo distribution

When B2B splits from this repo, distribution shifts but the C# stays the same:

| Artefact | Monorepo today | Split-repo |
|---|---|---|
| `Concertable.B2B.X.Contracts` | `ProjectReference` | Private NuGet package, `PackageReference Version="..."` |
| `Concertable.B2B.Seed.Contracts` | `ProjectReference` | Private NuGet package (consumed by Customer.E2ETests + simulator) |
| `Concertable.B2B.Seed.Simulator` | `AddProject<Projects.X>()` in Customer.AppHost | Container image, `AddContainer("b2b-seeding-simulator", "<registry>/concertable-b2b-seeding-simulator", "<tag>")` |

NuGet feed candidates: GitHub Packages, Azure Artifacts, MyGet. Container registry candidates: GHCR, Azure Container Registry, Docker Hub.

The simulator must ship as a container in the split because Aspire's `AddProject<>` resolves a `Projects.X` type generated at AppHost build time from a project reference — that source isn't available in Customer's repo if B2B is in another repo. Containers don't have that constraint.

Customer.AppHost's single line change:

```csharp
// Monorepo today
builder.AddB2BSeedingSimulator<Projects.Concertable_B2B_Seeding_Simulator>(asb);

// Split-repo tomorrow
builder.AddContainer("b2b-seeding-simulator", "ghcr.io/<org>/concertable-b2b-seeding-simulator", "1.2.0")
       .WithReference(asb);
```

Everything else — fixture import, handler code paths, projection logic — is unchanged.

## Boundary checks

If you're not sure you've kept things on the right side of the line, these grep commands catch the common violations:

- **No direct projection seeding anywhere:**
  ```
  grep -rn '\.Add(Range)?\s*(' --include='*.cs' api/ | grep -iE 'ReadModel' | grep -v 'obj/' | grep -v 'Migrations/'
  ```
  Should return zero hits.

- **Customer.Seeding has no B2B knowledge:**
  ```
  grep -rn 'Concertable\.B2B' api/Concertable.Customer/Concertable.Customer.Seed/
  ```
  Should return zero hits.

- **Simulator has no Domain references:**
  ```
  grep -n 'ProjectReference' api/Concertable.B2B/Concertable.B2B.Seed.Simulator/Concertable.B2B.Seed.Simulator.csproj
  ```
  Should show only `Contracts`, `Seeding.Fixture`, `Messaging`, `ServiceDefaults`. No Domain, no Seeding, no Application, no Infrastructure.

- **Tight sync check (manual):** start umbrella AppHost; query `CustomerDb` for venue id 1, artist id 2, concert id 13; record every column. Start standalone Customer AppHost; query the same three rows; columns should be identical (modulo `DatePosted` which is time-relative — same shape, different absolute value). Drift on any field means a value is being set somewhere other than `SeedCatalog`.

## Related docs

- [`api/ARCHITECTURE.md`](../../../ARCHITECTURE.md) — microservice premise.
- [`api/docs/SEEDING_CONVENTIONS.md`](../../../docs/SEEDING_CONVENTIONS.md) — the no-direct-projection-seeding rule and the rest of the seeding conventions.
- `api/Concertable.B2B/Seed/Concertable.B2B.Seed.Infrastructure/` — B2B's own SeedState (consumes the catalog for venues/artists/concerts) plus the `Factories/` (`VenueFactory`/`ArtistFactory`/`ConcertFactory`).
- `api/Concertable.B2B/Seed/Concertable.B2B.Seed.Contracts/` — the canonical `XSeedSpec` records and their `ToChangedEvent()` conversion.
- `api/Concertable.Customer/Concertable.Customer.AppHost/Program.cs` — where the simulator is registered.
