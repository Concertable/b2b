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

### E2E boots the whole real fleet from source references (won't survive the repo split)

`Concertable.B2B.E2ETests/AppFixture.cs` launches `Concertable.B2B.AppHost` via
`DistributedApplicationTestingBuilder.CreateAsync<Projects.Concertable_B2B_AppHost>()`, which composes
**real** Payment + Auth + Search through `Projects.Concertable_*` *source* references. That's fine in
the monorepo, but it's full-fleet E2E run from inside one service's repo — it conflates two test tiers
and breaks at the repo split (the `Projects.Concertable_Payment_*` types vanish once Payment is a
separate repo). E2E must never stub Payment (stubbing defeats E2E), so the fix is not "fake it here" —
it's to split the tiers by *where they run*:

**Resolves when:**
- **Per-repo (every PR):** B2B keeps only its **integration** tests, with the adapter services faked
  behind their contracts — Payment via the existing `MockManagerPaymentClient` / `MockEscrowClient` /
  `MockCustomerPaymentClient` against `Payment.Contracts` — plus **consumer-driven contract tests** so
  the fakes can't silently drift. No Payment source or runtime needed.
- **Full-fleet system E2E (rare / pre-release, centralised — not per-service-repo):** stands up the
  real fleet from **published container images** (`AddProject<Projects.Concertable_Payment_Web>()` →
  `AddContainer("payment", "<registry>/payment:<version>")`). Same real Payment, pulled not compiled.
  This suite moves out of B2B's repo into a system/deployment pipeline.

See [`plans/SPLIT_TIME_E2E_STRATEGY.md`](../../plans/SPLIT_TIME_E2E_STRATEGY.md).

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

### B2B integration fixture boots Payment in-process on a shared DB

`Concertable.B2B.IntegrationTests.Fixtures/ApiFixture.cs` registers `AddPaymentInfrastructure`, a `PaymentDbContext` bound to the same connection string as `B2BDb`, and `AddPaymentTestSeeder`. `MockEscrowClient` writes `EscrowEntity` rows straight into `PaymentDbContext`, and `MockWebhookSimulator` resolves and fires Payment's own `IIntegrationEventHandler<PaymentSucceededEvent>` (`PaymentTransactionHandler`) in-process. The B2B integration suite therefore runs B2B + Payment as a mini-monolith over one database — a microservice-isolation violation confined to the test harness. (Production B2B no longer touches Payment internals: after the Payment-agnostic refactor, `ReadDbContext` exposes no Payment entities and escrow reads go through the fixture's `PaymentDbContext`, not B2B's read context.)

**Resolves when:** `MockEscrowClient` / `MockManagerPaymentClient` / `MockCustomerPaymentClient` are pure in-memory contract mocks (return `Payment.Client` response types and record call args; no `PaymentDbContext`); `AddPaymentInfrastructure`, the `PaymentDbContext` registration, `AddPaymentTestSeeder`, and the shared `PaymentDb` connection string are removed from the B2B fixture; `MockWebhookSimulator` fires only B2B's `PaymentSucceededEvent` handlers; escrow/transaction *persistence* assertions move into Payment's own integration tests while B2B asserts on recorded mock call args (payer/payee/booking) instead of `fixture.Escrows`; and the `InternalsVisibleTo` from `Concertable.Payment.Application` / `.Infrastructure` to the B2B test projects are dropped.

---

## RESOLVED

### ✅ Seed `TicketsSold` depends on the Payment seed simulator

Decided in favour of **reflection-set** (`plans/PAYMENT_SEED_REFLECTION_REFACTOR.md`). `ConcertFactory`
now sets `ConcertEntity.TicketsSold` via `.With(nameof(ConcertEntity.TicketsSold), spec.TicketsSold)`
from a `ticketsSold` field on `ConcertSeedSpec`, so seed concerts carry a deterministic sold count with
no event round-trip and no dependency on a Payment seed simulator (which no longer exists). The
divergence-from-production concern is accepted here because past-dated ticket sales are **inherently
unreproducible** — real Payment only emits `PaymentSucceededEvent` for live Stripe webhooks, and you
can't buy a ticket to a concert that already happened. Documented as a sanctioned exception in
`docs/SEEDING_CONVENTIONS.md`. The settlement E2E (`ConcertFinishedTests`) reads these via
`TicketsSold * Price`: Past DoorSplit (id 12) and Past Versus (id 9) are seeded `ticketsSold: 1` —
the Versus concert was a real gap the old simulator catalog (concerts 13/12/10) omitted.

---

## LOW

### Intra-B2B events round-trip ASB

Concert's read-model sync from `ArtistChangedEvent`/`VenueChangedEvent` and User's manager sync handlers consume events via the bus inbox rather than in-process domain events. Plan §8.5 says intra-service flows should stay in-process via `IEventRaiser`.

**Resolves when:** The Concert and User module handlers for these events are wired to `IEventRaiser` in-process dispatch, and the ASB subscriptions for these intra-service uses are removed.
