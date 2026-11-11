# Concertable.B2B.Seed.Simulator

This one-shot development host publishes B2B-owned synthetic facts through the normal transport so
standalone consumers can populate their projections without running B2B. It has no B2B database and
is not an authoritative database backfill or an event-history replay service.

## Source and callers

`../Concertable.B2B.Seed.Contracts/SeedCatalog*.cs` owns the synthetic values and stable entity IDs.
`SeedEventPublisher.cs` publishes manager payout-owner registrations, venues, artists and concerts,
then stops the host. `../../Concertable.B2B.Hosting/B2BSeedingSimulator.cs` owns Aspire registration.
Search and Customer standalone AppHosts consume the published simulator image. Keep it out of the
full System composition when real B2B publishes its own seeded facts.

B2B's `../Concertable.B2B.Seed.Infrastructure/SeedState.cs` constructs its real domain seed from the
same catalog, with separate factories and domain-event mapping. Shared input does not prove complete
payload equality. `ConcertSeedParityTests` checks the concert ID set against that domain seed.
Booking 6 awaits payment and has no concert; preserve the remaining IDs rather than renumbering them.

## Run and verify

Use the owning standalone AppHost and its existing local Service Bus emulator. Inspect it through the
Aspire CLI or MCP interface. For example, from the Search checkout:

```powershell
aspire describe --apphost local/AppHost/Concertable.Search.AppHost.csproj
aspire logs b2b-seeding-simulator --tail 30 --apphost local/AppHost/Concertable.Search.AppHost.csproj
```

Check actual consumer projections and a representative public query after publishing. Process exit
and accepted sends do not prove projection convergence. Wait-for-Workers controls startup ordering;
it does not acknowledge processing of every event. A registry denial occurs before simulator execution
and should be diagnosed as package access, separately from publication or consumer failures.

Each run captures one time value for coherent relative concert dates, but a later run captures a new
value and creates new message IDs. Sequential sends do not guarantee processing order across topics.
Consumers must handle duplicate deliveries and upserts. A failed send can leave a partial publication;
there is no checkpoint, snapshot-completion protocol, or explicit simulator failure-exit contract.
Those behaviors require runtime verification before relying on automated recovery.

The host currently has no emulator-only connection guard. Use only the verified local AppHost; never
point synthetic events at a live broker. Read models must remain consumer-handler-owned: do not add
consumer database inserts to bypass the event path.

## Changing the catalog

Change the producer-owned specs and preserve their domain-seed parity. Update both the simulator image
and matching Seed.Contracts package through the existing release owner before claiming consumers use
a change. Existing stores are not reconciled by removing a catalog entry: the simulator publishes
upserts, not tombstones. Corrected catalog verification requires a fresh disposable store or an explicit
owner-approved reconciliation of retained synthetic data; never silently delete a database or volume.

Producer-owned authoritative backfill is a separate recovery capability. It would read B2B state and
publish snapshots with version/order, deletion and completion semantics. It would still need synthetic
input when B2B is deliberately absent from standalone development.
