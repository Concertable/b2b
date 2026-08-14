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

See [`plans/platform/SPLIT_TIME_E2E_STRATEGY.md`](../../plans/platform/SPLIT_TIME_E2E_STRATEGY.md).

---

## MED

### B2B outbound email is still synchronous inline — not on the transactional outbox

The async-email-outbox refactor put Auth (verification/reset) and Customer (ticket receipt) on the
transactional outbox (`IEmailSender` → `OutboxEmailSender` → `SendEmailCommand`), but **B2B's two email
producers still send synchronously** through `IEmailTransport` (the raw SMTP/fake send), so a transient
failure still loses the mail and the send isn't atomic with the business change:

- `Concert.Infrastructure/Services/Messenger` — the counterparty email on a conversation message/action.
- `Tenant.Infrastructure/Services/InvitationService` — the org-invitation email after the invitation saves.

They were left synchronous because the integration-test harness can't deliver an outbox command
in-process (`LocalDispatchingBus` deliberately doesn't dispatch commands locally, and the fixtures'
`MockBusTransport.SendAsync` is a no-op), so the `EmailSender.Sent` assertions in
`ApplicationCancel`/`ApplicationWithdrawReject`/`Invitation` tests only observe a synchronous send.
`Messenger` also has no clean transactional anchor — it fires off a conversation action, not a persisted
lifecycle transition.

**Resolves when:** the concert-lifecycle transition (and the conversation action) raise a domain event
whose pre-commit handler stages a `SendEmailCommand` on the same transaction (the
`TicketPurchasedDomainEventHandler` pattern), making B2B email transactional/retried like Auth and
Customer — with the B2B email integration assertions moved to draining the outbox (or asserting the
staged command) rather than a synchronous `Sent` list.

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
`agents/SEEDING_CONVENTIONS.md`. The settlement E2E (`ConcertFinishedTests`) reads these via
`TicketsSold * Price`: Past DoorSplit (id 12) and Past Versus (id 9) are seeded `ticketsSold: 1` —
the Versus concert was a real gap the old simulator catalog (concerts 13/12/10) omitted.

---

## LOW

### Application affordances are not yet modelled as role-and-state discriminated unions

Application responses need different affordances for venue and artist callers, and those affordances also vary by
application lifecycle state. The current non-preview design uses `ApplicationResponse<TActions>` with separate venue
and artist action objects; nullable links within each role-specific object intentionally mean that an action is not
available in the current state. This keeps the two actor cases separate, but the type system still permits invalid
combinations such as checkout and withdraw being populated together.

**Resolves when:** after the repository upgrades to a .NET/C# version with production-ready discriminated unions and
stable `System.Text.Json` / OpenAPI support for them, replace the role-specific nullable action objects with exhaustive
role-and-state unions. Each variant must carry only its valid links, and the API mapper plus TypeScript contracts must
handle every variant exhaustively so invalid affordance combinations are unrepresentable end to end.

---

### Contract PDFs share the `images` blob container and rely on app-level write-once

`ContractPdfService` stores contract PDFs under a `contracts/{bookingId}-{guid}.pdf` name in the **single shared `"images"` container** (the only container `Concertable.Shared.Blob` exposes). The blob *name* is fixed at creation, transactionally, at Accept (`ContractEntity.Create`), so generation can't race to mint competing names — but immutability of the *bytes* is still only app-level: `IBlobStorageService.UploadAsync` is `overwrite: true`, so nothing at the storage layer prevents a rewrite of a persisted legal document. A legal artefact ideally lives in its own container with a no-overwrite (write-once / immutability-policy) upload. Deliberately not done in the contract feature because both are **additive changes to the published `Concertable.Shared.Blob` package** (a dedicated container config + an overwrite-guarding `UploadAsync` overload), which would cross the package boundary the feature was scoped to avoid.

**Resolves when:** `Concertable.Shared.Blob` gains a dedicated-container + write-once upload path, contract PDFs move to it, and `AttachPdf`'s app-level guard is backed by a storage-level immutability guarantee.

---

### `ContractEntity` "created only at Accept" is convention, not an enforced invariant

`ContractEntity`'s terms are immutable once built (private setters + `Create` factory), but nothing binds `Create` to the Accept transition — that timing lives in `ContractIssuer`/`AcceptExecutor`, so a future caller could mint a contract outside Accept and the model wouldn't stop them. `VenueTenantId`/`ArtistTenantId` are also publicly settable (for the tenant interceptor + issuer), so the snapshot isn't fully sealed either. Not addressed in the DEAL_RENAME refactor, which was names-only.

**Resolves when:** the Accept aggregate owns contract creation (e.g. `Create` becomes internal to the transition, or the booking aggregate is the only path that can produce one), and the tenant fields are stamped through a constructor/interceptor seam rather than public setters.

---

### `deal.Fee`/`HireFee` are `decimal` domain fields lifted to `Money` at the payment boundary

The money value-type migration (PR1 #390 → sync #393) made every
payment-client + `ISettlementAmountResolver` signature `Money`-typed, but `FlatFeeDeal.Fee` /
`VenueHireDeal.HireFee` (contracts + `*DealEntity`) stayed `decimal`. The workflow steps (`HoldCheckoutStep`,
`Capture`/`DepositEscrowAcceptStep`) lift them with `Money.Gbp(deal.Fee)` at the call sites — a legitimate
boundary conversion (same pattern as Customer's `Money.Gbp(concert.Price * qty)`), but it assumes GBP and keeps
a money value untyped in the domain, inconsistent with `EscrowEntity.Amount` which is a `Money` EF
ComplexProperty. Deferred from the sync PR because the field-type change needs an EF ComplexProperty mapping +
a DB re-scaffold that couldn't be verified in the disk/MAX_PATH-constrained environment at the time.

**Resolves when:** `Fee`/`HireFee` become `Money` (contracts + entities), mapped as a ComplexProperty like
`EscrowEntity.Amount`, the deal mappers + read sites cascade, migrations are re-scaffolded, and the
`Money.Gbp(deal.Fee)` boundary lifts collapse to plain `deal.Fee`.

---

### VAT / seller-id validation is format-only (regex), not verified against an authority

`UkDac7Strategy.IsValidVatNumber` checks only the *shape* of a VAT number (a regex from `UkDac7Options.VatNumberPattern`) — it proves the value looks like a UK VAT number, not that it's a real, active registration. DAC7's obligation is to *collect and verify* seller tax identity; format-only is the weak end of "verify". Stronger options, all pluggable behind the existing per-region `IDac7Strategy` seam without touching the gate / nag / form: (1) an offline **checksum** — UK VAT numbers carry a mod-97 check digit — to catch typos a regex passes; (2) **live verification** — HMRC's "Check a UK VAT number" API (returns a consultation reference number, itself useful audit evidence for the 2028 export) or, for EU sellers, VIES. Before building our own, check what **Stripe Connect** already collects/verifies on connected accounts — we may be about to re-solve tax-ID verification Stripe already does.

Deliberately not done now: the launch gate is *data completeness* (hold a complete, jurisdiction-valid tax identity for everyone we pay), not live verification. Live checks are async/networked (need caching + graceful degradation) and overlap Stripe — scope this onboarding blocker doesn't take on. Naturally lands with the DAC7 verification/export hardening (first export Jan 2028).

**Resolves when:** VAT (and other seller-id) validity is checked beyond format per jurisdiction — minimally an offline checksum, ideally a live authority check (HMRC / VIES) or a confirmed reuse of Stripe's tax-ID verification — implemented as the per-region `IDac7Strategy` behaviour, with the stored value staying a lenient `string?`.

---

### B2B portal frontend URLs have no non-local config — prod invite links would break

`FrontendUriGenerator` (`Concertable.B2B.Infrastructure`) resolves the venue/artist portal base per tenant type from `Urls:Frontends:{Venue,Artist}`. Those keys exist only as **localhost** in `Concertable.B2B.Web/appsettings.json`; there is no per-environment (App Config / tfvars) source for the real `venue.`/`artist.concertable.co.uk` hosts — that whole cloud-config layer is still the blocked future work in [`../../plans/platform/DOMAINS_AND_DNS.md`](../../plans/platform/DOMAINS_AND_DNS.md). So in any non-local environment the tenant-type dictionary binds empty and an invite send throws `KeyNotFoundException` — fails loud (not a silent bad link), but still broken.

**Resolves when:** `Urls:Frontends:{Venue,Artist}` are supplied per environment from App Config, alongside `Auth:SpaClients` / `Cors:AllowedOrigins` (which key off the same hostnames), as part of the `DOMAINS_AND_DNS.md` config rollout.

---

### Integration tests pass `(object?)null` to bodyless `PostAsync` instead of the parameterless overload

The B2B integration suites call `client.PostAsync(url, (object?)null)` for bodyless action POSTs
(`withdraw`/`reject`/`cancel`/`accept`) — ~22 sites across `Concertable.B2B.Concert.IntegrationTests`
(`ApplicationApiTests`, `ApplicationWithdrawRejectApiTests`, `ApplicationCancelApiTests`).
`Concertable.Testing.HttpClientExtensions` already exposes a parameterless `PostAsync(this HttpClient,
string url)` that posts the identical null JSON body (`PostAsJsonAsync<object?>(url, null)`), so the
`(object?)null` cast is redundant ceremony that spread by copy-paste. Behaviour is identical — a
readability nit, left uniform for now rather than migrating a lone call site out of step with its siblings.

**Resolves when:** the `PostAsync(url, (object?)null)` sites switch to the parameterless `PostAsync(url)`
in one mechanical sweep (no behaviour change).
