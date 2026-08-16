# Concertable.B2B — Technical Debt

When an item is fixed, update both this file and [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## HIGH

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

### B2B counterparty email (`Messenger`) is still synchronous inline — not on the transactional outbox

The async-email-outbox refactor put Auth (verification/reset), Customer (ticket receipt), and **B2B's
org-invitation email** on the transactional outbox: an `IPreCommitDomainEventHandler` stages a
`SendEmailCommand` on the same transaction as the business change (the `TicketPurchasedDomainEventHandler`
pattern). `Tenant.Infrastructure/Services/InvitationService` now raises `TenantInvitationCreatedDomainEvent`
whose pre-commit handler stages the invite mail, anchored on the invitation save — done.

One B2B producer remains synchronous through `IEmailTransport` (the raw SMTP/fake send), so a transient
failure still loses the mail and the send isn't atomic with the business change:

- `Concert.Infrastructure/Services/Messenger` — the counterparty email on a conversation message/action.
- `Concert.Infrastructure/Services/BookingConfirmationNotifier` — the both-party booking-confirmation email at concert-draft creation (`ConcertDraftService.CreateAsync`).

`Messenger` has no clean transactional anchor of its own — it fires a conversation *action*, not a persisted
lifecycle transition, so it can't simply mirror the invitation fix. The lifecycle executors that drive it
(`ApplicationCancel`/`ApplicationWithdrawReject`) *do* persist a transition, so the anchor is that
transition's domain event, not `Messenger` itself. Their `EmailSender.Sent` integration assertions still
observe a synchronous send.

**Resolves when:** the concert-lifecycle transition raises a domain event whose pre-commit handler stages
the counterparty `SendEmailCommand` on the same transaction, making it transactional/retried like the
invitation email — with the `ApplicationCancel`/`ApplicationWithdrawReject` email assertions moved to
asserting the staged command (`fixture.GetStagedEmailsAsync()`) rather than a synchronous `Sent` list.

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

---

### The `[Admin]` authorization seam is thin, and there is no admin UI for moderation

`AdminAttribute` (`User.Api/Authorization`) resolves an `AdminProfileEntity` — a bare `Sub` column with
no roles and no scoping — through `AdminProfileHandler`, which issues an **uncached `UserDbContext`
query on every request** to every `[Admin]` endpoint. Admin provisioning only happens via registration
through the `admin` client-id (`CredentialRegisteredHandler`) or `UserTestSeeder`. Until the OSA
report-content work it was applied in exactly one place (`VenueController.Approve`); it now also gates
`ModerationController` (hide / restore / resolve / triage queue).

As an *authorization axis* this is correct and sufficient — it answers "is this caller a platform
operator?", which is precisely what those endpoints ask, and it is deliberately not tenant RBAC
(a `TenantRole` is scoped to one tenant and must never let a venue Owner moderate someone else's
thread; an integration test asserts a tenant Owner gets 403 on every moderation endpoint). As an
*operations surface* it is not sufficient:

- **No admin SPA**, so moderation is Swagger/curl-driven at launch.
- **No admin roles**, so every operator has every admin capability.
- **A per-request uncached DB hit** on each `[Admin]` call.

The moderation feature compensates in its own data rather than by growing the seam: every action stamps
the acting user id and timestamp onto the report record, so the audit trail exists regardless. Accepted
at the expected near-zero report volume.

**Resolves when:** admin identity gains roles/scoping and a cached lookup, and an admin surface exists
to drive moderation — at which point the Swagger/curl workaround and this entry both go.

---

### Conversations has no thread aggregate, no per-thread read, and no retention policy

A "thread" in Conversations is implicit — it is whatever shares a `(VenueTenantId, ArtistTenantId)`
pair. There is a `MessageEntity` and a `ThreadReadStateEntity` but no `ThreadEntity`, and consequently:

- **No per-thread view exists.** `GetByTenantIdAsync` returns one flat inbox ordered by `SentDate`
  across every counterparty. That is right for the notification bell it currently feeds and wrong the
  moment anyone wants an actual conversation UI.
- **`AdvanceReadPointersAsync` is O(threads) per call** — it loads every distinct pair, loads every
  pointer for the member, then loops in memory. Invisible at ten threads, not at a thousand.
- **Messages accumulate forever.** Nothing prunes them, and the Online Safety Act work deliberately
  hides rather than deletes, so hidden content accumulates too.

The storage choice itself is not the debt — a relational store is correct for booking correspondence
that must be transactional with the booking flow and queryable for a regulator, and the specialised
stores chat products use would trade away exactly the properties this needs. The debt is the missing
aggregate and the missing lifecycle.

**Resolves when:** a thread aggregate exists with a per-thread paged read, the read-pointer advance is
a set-based update rather than a per-pair loop, and a retention policy is implemented — the last of
which is gated on the solicitor-owned retention artifact in the OSA compliance pack, so it cannot be
invented here.

---

### Content reporting is modelled as message-only and will not generalise as-is

`ContentReportEntity` lives in Conversations because a `MessageEntity` is the only reportable artifact
today, which is correct now and deliberately not abstracted early. But the Online Safety Act duty
attaches to **user-generated content**, and this platform has more of it: venue and artist profile text,
concert descriptions, uploaded images, and customer reviews. The Customer/marketplace OSA scope is
explicitly deferred with the marketplace, which is when those become in-scope.

The entity will not stretch to cover them. It carries a typed `MessageId` and is
`IVenueArtistTenantScoped` — it holds a **thread pair**. A report against a venue profile has no thread
pair, so neither the foreign key nor the tenancy shape fits.

**Resolves when:** a second reportable content type is actually required, at which point choose
deliberately between a polymorphic `(ContentType, ContentId)` report with per-type tenancy resolution,
or a per-module report entity behind a shared triage view. Do not pre-build either before the second
case exists.
