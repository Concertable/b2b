# Concertable.B2B — Technical Debt

When an item is fixed, update both this file and [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## HIGH

### Accept flow is not atomic — booking + escrow charge can persist without the application transition

The accept path (`AcceptExecutor.ExecuteAsync` → `LifecycleTransitioner.TransitionAsync`) is **not wrapped in a transaction**. Inside the transition effect, `BookingService.CreateStandardAsync` commits the booking with its own `SaveChangesAsync`, then `CaptureEscrowAcceptStep` / `DepositEscrowAcceptStep` initiate the Stripe escrow charge (`IEscrowClient.Capture/Deposit`). Only afterwards does `TransitionAsync` save the application state transition in a *separate* `SaveChangesAsync`. A failure after the charge (the final transition save, `app.Accept`, or `RejectAllExcept`) leaves a **committed booking and a charged/held card while the application is never transitioned to Accepted** — an inconsistent state needing manual reconciliation, and a retry risks double-charging.

Note — this is an **application** issue, not a pipeline one. Because the booking is committed *before* the charge today, the escrow `PaymentSucceededEvent` webhook always finds it in production, so there is **no escrow race in prod**. The "Booking not found" errors in the E2E logs are a *test-isolation artifact* (per-test DB reset via Respawn + a single shared async Stripe `listen` webhook stream → an earlier test's webhook arrives after its rows were wiped). Don't conflate the two.

**Resolves when:** the accept flow runs in a single `UnitOfWorkBehavior` transaction (booking + application transition commit atomically) **and** the escrow Capture/Deposit is deferred to the transactional outbox — staged in the same transaction, dispatched only after commit — so making accept atomic does not reintroduce the webhook race (the booking is durable before Stripe is told to charge). Concretely: enqueue a `CaptureEscrow` / `DepositEscrow` `IIntegrationCommand` in the accept transaction, with a post-commit `IIntegrationCommandHandler` performing the gRPC charge (mirrors Payment's `ProcessStripeWebhookCommand`).

---

### Workers uses `AddInMemoryTransport`, not ASB

`Concertable.B2B.Workers/ServiceCollectionExtensions.cs` line 35 wires `services.AddInMemoryTransport()`. The Workers host cannot consume any cross-service events from the bus. Settlement triggers and payout reconciliation that belong in Workers run inside `Concertable.B2B.Web` today.

**Resolves when:** `ServiceCollectionExtensions.cs` calls `services.AddAzureServiceBusTransport(...)` with `ServiceName = "concertable-b2b"` and subscribes the relevant events (`PaymentSucceededEvent`, etc.) to the Workers handlers.

---

### No `ConcertSalesProjection`

There is no sold-count / gross-revenue projection. B2B dashboards and settlement math can't read authoritative ticket sales data from Customer.

**Depends on:** Customer publishing `TicketPurchasedEvent` (see `api/Concertable.Customer/TECH_DEBT.md`).

**Resolves when:** `TicketPurchasedEvent` exists in Customer; B2B.Workers subscribes and writes a `ConcertSalesProjection` entity (concertId, soldCount, grossRevenue) into B2B DB, owned and read by the Concert module via its own context.

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

### `IgnoreQueryFilters` used to subtract tenancy instead of composing a stance — anti-pattern

`CODE_PATTERNS.md` § "Tenancy is composed, never subtracted" bans per-query `IgnoreQueryFilters` and
claims the codebase has **zero** — but two had leaked in (both pre-existing on `master`). The right
alternatives were always available and are named in that doc: read cross-tenant through a composed
**public stance** (`PublicXDbContext`), expose a cross-tenant *fact* as a **boolean/scalar named
abstraction** on the public stance (e.g. `IConcertAvailability`), or **resolve the ids and pass them
in** at the call site (as B2B fronts Payment). Never `.IgnoreQueryFilters()`.

- **Fixed (`Feature/VatAndSelfBilledInvoicing`):** `ContractRepository.GetByBookingIdIgnoringTenantAsync`
  existed only to feed an eager background PDF render with no tenant context. Removed with the eager
  path entirely — contract + invoice PDFs now render lazily on download (a tenant-scoped party request),
  so there is no context-free read to bypass a filter for. See the render-timing decision below.
- **Outstanding:** `BookingRepository.ExistsIgnoringTenantAsync`, used by
  `EscrowExecutor.LoadApplicationIdAsync` to word a diagnostic ("exists ignoring tenant filter: …") when
  the tenant-filtered lookup misses in the escrow **payment-webhook** path. This is a genuine
  cross-tenant boolean *fact*, so the composed fix is a boolean-only named abstraction on the public
  stance (à la `IConcertAvailability`) — but it sits in money-path code whose webhook tenant-context
  model wants understanding first, so it was left out of the invoicing PR.

**Resolves when:** `BookingRepository.ExistsIgnoringTenantAsync` is replaced by a composed public-stance
existence check (no `IgnoreQueryFilters`), the escrow diagnostic reads through it, and the
`CODE_PATTERNS.md` "zero `IgnoreQueryFilters`" claim is true and re-affirmed with the alternatives spelled
out (the convention discussion this entry is the placeholder for).

---

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

### `DELETE api/organizations` is a local hard-delete with no cross-module / cross-service teardown

`TenantService.DeleteCurrentTenantAsync` deletes the tenant row and cascades only the Tenant module's own children (memberships, invitations). It emits **no `TenantDeletedEvent`** and touches nothing outside the `tenant` schema, so deleting an organization silently **orphans** everything provisioned off it: the Payment Stripe payout account (provisioned by `CredentialRegisteredHandler`), the venues/artists/concerts owned by the tenant (separate modules/contexts, no cross-schema FK — so no error, just dangling rows), and downstream Search projections. The create path deliberately re-raises `TenantCreatedEvent` via `Announce()` for exactly this cross-service reason; delete has no symmetric path. Landed as a simple synchronous endpoint in the member-management phase (Phase 6.2); the full teardown is its own design (a new integration event + a Payment consumer that deactivates the connected account + module-owned cleanup of venue/artist/concert data).

**Resolves when:** tenant deletion publishes a `TenantDeletedEvent` (registered `Publishes<>`), Payment deactivates/closes the connected Stripe account on it, the Venue/Artist/Concert modules clean up (or soft-delete) their tenant-owned rows via their own handlers, and Search drops the corresponding projections — no owned data outlives the tenant.

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

### Contract PDFs share the `images` blob container and rely on app-level write-once

`ContractPdfService` stores contract PDFs under a `contracts/{bookingId}-{guid}.pdf` name in the **single shared `"images"` container** (the only container `Concertable.Shared.Blob` exposes). The blob *name* is fixed at creation, transactionally, at Accept (`ContractEntity.Create`), so generation can't race to mint competing names — but immutability of the *bytes* is still only app-level: `IBlobStorageService.UploadAsync` is `overwrite: true`, so nothing at the storage layer prevents a rewrite of a persisted legal document. A legal artefact ideally lives in its own container with a no-overwrite (write-once / immutability-policy) upload. Deliberately not done in the contract feature because both are **additive changes to the published `Concertable.Shared.Blob` package** (a dedicated container config + an overwrite-guarding `UploadAsync` overload), which would cross the package boundary the feature was scoped to avoid.

**Resolves when:** `Concertable.Shared.Blob` gains a dedicated-container + write-once upload path, contract PDFs move to it, and `AttachPdf`'s app-level guard is backed by a storage-level immutability guarantee.

---

### `ContractEntity` "created only at Accept" is convention, not an enforced invariant

`ContractEntity`'s terms are immutable once built (private setters + `Create` factory), but nothing binds `Create` to the Accept transition — that timing lives in `ContractIssuer`/`AcceptExecutor`, so a future caller could mint a contract outside Accept and the model wouldn't stop them. `VenueTenantId`/`ArtistTenantId` are also publicly settable (for the tenant interceptor + issuer), so the snapshot isn't fully sealed either. Not addressed in the DEAL_RENAME refactor, which was names-only.

**Resolves when:** the Accept aggregate owns contract creation (e.g. `Create` becomes internal to the transition, or the booking aggregate is the only path that can produce one), and the tenant fields are stamped through a constructor/interceptor seam rather than public setters.

---

### Concert response family names are over-qualified

The `Concert.Api.Responses` types stack redundant qualifiers — `ConcertDetailsResponse`,
`ConcertSummaryResponse`, `ConcertArtistResponse`, `ConcertVenueSummaryResponse`, etc. — re-stating `Concert`
(already the namespace) and vague words like `Details`. The `Response` suffix is mandated (it marks the HTTP
wire layer); the rest is bloat. (Splitting the public vs owner reads into separate types was considered and
**declined** — the single response with owner-only fields populated only by the owner mapper is safe and is
the same role-shaping pattern `ApplicationResponse` already uses; not worth a one-off divergence.)

**Resolves when:** the response family is de-verbosed in one pass — drop the redundant `Concert`/`Details`
qualifiers where the namespace already carries them, keep `Response` — and the SPA's consumed/generated type
names are updated to match.

---

### `UserEntity.Avatar` models "no avatar" as empty string

`Modules/User/Concertable.B2B.User.Domain/UserEntity.cs` declares `public string Avatar { get; private set; } = string.Empty;` — an empty-string placeholder pretending to be a value (the pattern `docs/CODE_CONVENTIONS.md` bans for populated-later defaults). "No avatar" is modelled as `string?` elsewhere (e.g. `ConcertArtistResponse.Avatar`).

**Resolves when:** `Avatar` becomes `string?` with no default, consumers null-check instead of empty-check, and the column is re-scaffolded nullable via `./initial-migrations.ps1`.

---

### Duplicate application attempt is a 500, not a 400 — guard landed, integration test outstanding

Fixed on `Fix/TechDebtSweep`: `ApplicationValidator.CanApplyAsync` now rejects an existing
`(opportunityId, artistId)` row via `IApplicationRepository.ExistsForOpportunityAndArtistAsync`,
returning a clean 400; a unit test covers it. Outstanding only: an **integration test** for
apply-after-withdraw → 400 (needs Docker).

**Resolves when:** the apply-after-withdraw integration test lands green.

---

### Intra-service read-model sync rides the bus instead of in-process dispatch

Concert's read-model sync from `ArtistChangedEvent`/`VenueChangedEvent` and User's manager sync handlers consume events via the bus inbox rather than in-process domain events. Plan §8.5 says intra-service flows should stay in-process via `IEventRaiser`.

**Resolves when:** The Concert and User module handlers for these events are wired to `IEventRaiser` in-process dispatch, and the ASB subscriptions for these intra-service uses are removed.

---

### VAT / seller-id validation is format-only (regex), not verified against an authority

`UkDac7Strategy.IsValidVatNumber` checks only the *shape* of a VAT number (a regex from `UkDac7Options.VatNumberPattern`) — it proves the value looks like a UK VAT number, not that it's a real, active registration. DAC7's obligation is to *collect and verify* seller tax identity; format-only is the weak end of "verify". Stronger options, all pluggable behind the existing per-region `IDac7Strategy` seam without touching the gate / nag / form: (1) an offline **checksum** — UK VAT numbers carry a mod-97 check digit — to catch typos a regex passes; (2) **live verification** — HMRC's "Check a UK VAT number" API (returns a consultation reference number, itself useful audit evidence for the 2028 export) or, for EU sellers, VIES. Before building our own, check what **Stripe Connect** already collects/verifies on connected accounts — we may be about to re-solve tax-ID verification Stripe already does.

Deliberately not done now: the launch gate is *data completeness* (hold a complete, jurisdiction-valid tax identity for everyone we pay), not live verification. Live checks are async/networked (need caching + graceful degradation) and overlap Stripe — scope this onboarding blocker doesn't take on. Naturally lands with the DAC7 verification/export hardening (first export Jan 2028).

**Resolves when:** VAT (and other seller-id) validity is checked beyond format per jurisdiction — minimally an offline checksum, ideally a live authority check (HMRC / VIES) or a confirmed reuse of Stripe's tax-ID verification — implemented as the per-region `IDac7Strategy` behaviour, with the stored value staying a lenient `string?`.
