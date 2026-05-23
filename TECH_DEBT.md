# Concertable.B2B — Technical Debt

When an item is fixed, update both this file and [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## HIGH

### Workers uses `AddInMemoryTransport`, not ASB

`Concertable.B2B.Workers/ServiceCollectionExtensions.cs` line 35 wires `services.AddInMemoryTransport()`. The Workers host cannot consume any cross-service events from the bus. Settlement triggers and payout reconciliation that belong in Workers run inside `Concertable.B2B.Web` today.

**Resolves when:** `ServiceCollectionExtensions.cs` calls `services.AddAzureServiceBusTransport(...)` with `ServiceName = "concertable-b2b"` and subscribes the relevant events (`PaymentSucceededEvent`, etc.) to the Workers handlers.

---

### No `ConcertSalesProjection`

`ReadDbContext` has no sold-count / gross-revenue projection. B2B dashboards and settlement math can't read authoritative ticket sales data from Customer.

**Depends on:** Customer publishing `TicketPurchasedEvent` (see `api/Concertable.Customer/TECH_DEBT.md`).

**Resolves when:** `TicketPurchasedEvent` exists in Customer; B2B.Workers subscribes and writes a `ConcertSalesProjection` entity (concertId, soldCount, grossRevenue) into B2B DB; `ReadDbContext` exposes `ConcertSalesProjections`.

---

## MED

### `Modules/User/` TPH not unwound

Plan §4.5 calls for flat per-persona profile tables (`VenueManagerEntity`, `ArtistManagerEntity`, `AdminEntity`) each carrying the Auth `sub`, with no shared `UserEntity` base via TPH. Current state of the `User.Domain` hierarchy needs verifying and may still be TPH.

**Resolves when:** The User module entities are flat tables without a TPH discriminator column; the `UserEntity` base row no longer carries persona-specific fields.

---

### Defined-but-not-published events

`ConcertSettledEvent`, `ConcertFinishedEvent`, `ConcertApplicationCreatedEvent`, `ConcertApplicationAcceptedEvent` exist in `Concertable.B2B.Concert.Contracts.Events` but are not registered as `Publishes<>` in `Program.cs` and are not raised anywhere.

**Resolves when:** Either (a) each event is raised from the appropriate domain event, registered in `Program.cs`, and consumers exist in Search/Customer; or (b) the event types are deleted as dead code.

---

### `Modules/Notification/` pending deletion

`Concertable.Shared.Email` is already wired by both B2B and Customer. The `Modules/Notification/` module (Contracts + Infrastructure) still ships and hosts the `NotificationHub` (SignalR). Email sending should already be routed through `IEmailSender` from the shared library.

**Resolves when:** Phase 8 Step 24 — SignalR hub moved to its own home; remaining email-only surface in `Modules/Notification/` removed; all callers use `IEmailSender` directly.

---

## LOW

### Intra-B2B events round-trip ASB

Concert's read-model sync from `ArtistChangedEvent`/`VenueChangedEvent` and User's manager sync handlers consume events via the bus inbox rather than in-process domain events. Plan §8.5 says intra-service flows should stay in-process via `IEventRaiser`.

**Resolves when:** The Concert and User module handlers for these events are wired to `IEventRaiser` in-process dispatch, and the ASB subscriptions for these intra-service uses are removed.
