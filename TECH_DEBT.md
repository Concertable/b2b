# Concertable.B2B — Technical Debt

When an item is fixed, update both this file and [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## HIGH

### Workers uses `AddInMemoryTransport`, not ASB

`Concertable.B2B.Workers/ServiceCollectionExtensions.cs` line 35 wires `services.AddInMemoryTransport()`. The Workers host cannot consume any cross-service events from the bus. Settlement triggers and payout reconciliation that belong in Workers run inside `Concertable.B2B.Web` today.

**Resolves when:** `ServiceCollectionExtensions.cs` calls `services.AddAzureServiceBusTransport(...)` with `ServiceName = "concertable-b2b"` and subscribes the relevant events (`PaymentSucceededEvent`, etc.) to the Workers handlers.

---

### Venue dashboard revenue reads a table nothing writes

`VenueDashboardService` (`GetAsync`, `GetPaymentRevenueAsync`) calls `IPaymentReportingClient.GetPaymentRevenueAsync` /
`GetPaymentRevenueByMonthAsync`, which sum `PaymentTransactionEntity` rows. The only writer of that entity is Payment's
`PaymentTransactionRecorder`, registered under the keyed value `TransactionTypes.Payment` (`"payment"`). Nothing in the
system ever emits that key: `PaymentSessionProviderRequest` stamps the metadata `type` with the operation's own
`OperationType`, and the only payment-kind operation is Customer's ticket purchase, whose type is `"ticket-purchase"`.
The venue revenue KPI and its six-month chart are therefore structurally zero, with no exception and no failing test —
`MockSettlementClient` hard-codes `Money.Gbp(0m)` and `[]`, so the B2B suite cannot see it.

This is not introduced by the v1 cut-over's renaming: the pre-v1 `GetTicketRevenueAsync` summed the same table against
the then-live `TransactionTypes.Ticket` key, which v1 deleted. B2B had no other reporting query to migrate to.

**Resolves when:** the venue revenue widgets read the `ConcertSalesProjection` below instead of Payment, or Payment keys
its transaction recorder on the operation kind rather than a `type` string no producer emits (and adds `AmountMinor` to
`PaymentSessionProviderRequest.MetadataOf`, which the recorder reads and nothing writes).

---

### Accept checkout mints a throwaway authorization operation id

`ApplicationCheckoutService` passes `Guid.CreateVersion7()` as the FlatFee authorization's `OperationId`, so
every GET of the accept checkout page mints a fresh id. Every other operation-id site in B2B is `??=`-stable
and uniquely indexed, and the accept path itself reuses `application.AcceptanceOperationId`.

It does not double-charge today only because Payment's `ReserveInitialAsync` catches the duplicate key on
`(OperationType, ClientReference)` and re-resolves the existing operation by reference. Correctness therefore
rests on Payment's fallback rather than on the reference B2B already owns and freezes.

Found by independent review during PR #633 (finding IR37).

**Resolves when:** the accept checkout passes the application's own acceptance operation id rather than a
fresh GUID, so the id is stable across reloads without relying on Payment's duplicate-key recovery.

---

### No `ConcertSalesProjection`

There is no sold-count / gross-revenue projection. B2B dashboards and settlement math can't read authoritative ticket sales data from Customer.

**Depends on:** Customer publishing `TicketPurchasedEvent` (see `api/Concertable.Customer/TECH_DEBT.md`).

**Resolves when:** `TicketPurchasedEvent` exists in Customer; B2B.Workers subscribes and writes a `ConcertSalesProjection` entity (concertId, soldCount, grossRevenue) into B2B DB, owned and read by the Concert module via its own context.

---

### E2E boots the whole real system from source references (won't survive the repo split)

`Concertable.B2B.E2ETests/AppFixture.cs` launches `Concertable.B2B.AppHost` via
`DistributedApplicationTestingBuilder.CreateAsync<Projects.Concertable_B2B_AppHost>()`, which composes
**real** Payment + Auth + Search through `Projects.Concertable_*` *source* references. That's fine in
the monorepo, but it's full-system E2E run from inside one service's repo — it conflates two test tiers
and breaks at the repo split (the `Projects.Concertable_Payment_*` types vanish once Payment is a
separate repo). E2E must never stub Payment (stubbing defeats E2E), so the fix is not "fake it here" —
it's to split the tiers by *where they run*:

**Resolves when:**
- **Per-repo (every PR):** B2B keeps only its **integration** tests, with the adapter services faked
  behind their contracts — Payment via the existing `MockManagerPaymentClient` / `MockEscrowClient` /
  `MockCustomerPaymentClient` against `Payment.Contracts` — plus **consumer-driven contract tests** so
  the fakes can't silently drift. No Payment source or runtime needed.
- **Full-system E2E (rare / pre-release, centralised — not per-service-repo):** stands up the
  real system from **published container images** (`AddProject<Projects.Concertable_Payment_Web>()` →
  `AddContainer("payment", "<registry>/payment:<version>")`). Same real Payment, pulled not compiled.
  This suite moves out of B2B's repo into a system/deployment pipeline.

Tracked by [`plans/platform/REPOSITORY_PER_MICROSERVICE_MIGRATION_PLAN.md`](../../plans/platform/REPOSITORY_PER_MICROSERVICE_MIGRATION_PLAN.md), stages 3 and 4.

---

## MED

### `Concertable.B2B.E2ETests/AppFixture.cs` hand-duplicates a subset of the real host's DI registrations, and can silently under-provision

The seed-only `Host` built in `AppFixture.cs` (`InitializeAsync`, ~line 157) re-lists its own subset of
`B2BWebHostExtensions.AddB2BWebHost`'s registrations rather than reusing it, because it only needs enough
to run `IDevSeeder`s. This drifted out of sync once: `ConcertDevSeeder` depends on `ITenantModule`, whose
graph (via `IVerificationService`) now reaches `IVenueModule`/`IArtistModule` → `VenueService`/
`ArtistService` → `IImageService` — a real, load-bearing dependency this seed host never registered
(`services.AddSharedImaging()` was missing; added alongside this note). Nothing catches this class of gap
at compile time; it only surfaces as an E2E `IDbInitializer` resolution failure the next time a module's
cross-module facade graph grows to reach a shared service this host omits.

**Resolves when:** the seed host's `ConfigureServices` is built from the same registration list as
`AddB2BWebHost` (e.g. factoring the shared-service registrations `AddB2BWebHost` and this fixture both
need into one call), so a facade graph reaching a new shared dependency can't silently leave one host
under-provisioned relative to the other.

---

### Venue opportunity counts are exposed by the write repository

`IOpportunityRepository.GetOpenWithApplicationCountsByVenueTenantIdAsync` is a read-only dashboard
projection, but it currently lives on the write repository and queries `ConcertDbContext` with
`AsNoTracking`. That is behaviourally safe, but it blurs the repository permission boundary and makes
`OpportunityDashboardService` depend on the write surface for a query. The projection belongs on
`IOpportunityReadRepository`, implemented by `OpportunityReadRepository` against
`IConcertReadDbContext`; the read context already provides the correct no-tracking stance, so this
should not be solved by restoring a general-purpose `Query` escape hatch.

**Resolves when:** the projection and its tests move to `IOpportunityReadRepository` /
`OpportunityReadRepository`, `OpportunityDashboardService` reads it through that interface, and
`IOpportunityRepository` no longer exposes the query.

---

### `DELETE api/organization` is a local hard-delete with no cross-module / cross-service teardown

`TenantService.DeleteAsync` deletes the tenant row and cascades only the Tenant module's own children (memberships, invitations). It emits **no `TenantDeletedEvent`** and touches nothing outside the `tenant` schema, so deleting an organization silently **orphans** everything provisioned off it: the Payment Stripe payout account (provisioned by `CredentialRegisteredHandler`), the venues/artists/concerts owned by the tenant (separate modules/contexts, no cross-schema FK — so no error, just dangling rows), and downstream Search projections. The create path deliberately re-raises `TenantCreatedDomainEvent` via `Announce()`, which `TenantCreatedDomainEventHandler` turns into the integration `PayoutOwnerRegisteredEvent`, for exactly this cross-service reason; delete has no symmetric path. Landed as a simple synchronous endpoint in the member-management phase (Phase 6.2); the full teardown is its own design (a new integration event + a Payment consumer that deactivates the connected account + module-owned cleanup of venue/artist/concert data).

**Resolves when:** tenant deletion publishes a `TenantDeletedEvent` (registered `Publishes<>`), Payment deactivates/closes the connected Stripe account on it, the Venue/Artist/Concert modules clean up (or soft-delete) their tenant-owned rows via their own handlers, and Search drops the corresponding projections — no owned data outlives the tenant.

---

### Architecture tests hardcode the module list instead of discovering it by reflection

`Concertable.B2B.ArchitectureTests/ModuleBoundaryTests.cs`'s `Modules` array and
`IntegrationTestBoundaryTests.cs`'s path/name filters name every module and sub-module by hand. This already
caused a real gap: the Dashboard module family shipped with zero boundary enforcement until a review caught it,
because nobody remembered to add it to the hardcoded list. The same class of gap exists for
`IntegrationTestBoundaryTests`'s `Process.IntegrationTests` special-case. A new module or test project is
silently unenforced until someone remembers to update these lists by hand.

**Resolves when:** both tests discover modules and their layer/sub-module structure by reflection over the
loaded `Concertable.B2B.*` assemblies (or by scanning `src/Modules/*` directories) instead of a maintained
string array, so a new module is enforced automatically the moment its assembly exists.

---

### Venue and Artist duplicate a four-method tenant-keyed surface with no shared seam

`IVenueRepository` and `IArtistRepository` each declare the same four tenant-keyed reads —
`GetByTenantIdAsync`, `GetDetailsByTenantIdAsync`, `ExistsByTenantIdAsync` and `GetContactByTenantIdAsync` —
differing only in the module-owned return type (`VenueEntity`/`ArtistEntity`, `VenueDetails`/`ArtistDetails`),
and `VenueService`/`ArtistService` consume them in matching shapes. Today the duplication sits *inside* each
module rather than as a branch in a key-agnostic consumer, which is why it reads as ordinary module
separation rather than a smell.

Two things make it debt rather than acceptable symmetry. A third `TenantType` would need a third verbatim
copy of the whole surface, so the shape has a scaling cliff nobody has priced. And there is no declared seam
for a cross-module consumer, so the next component needing "the venue-or-artist thing for this tenant" will
reach for dual-injection and a `TenantType` branch — which is exactly the debt just resolved in
`VerificationService`, re-created one module over.

What exists now is the `ITenantStrategy` / `ITenantStrategyFactory<TStrategy>` spine in the Tenant module
over the shared `KeyedStrategyBuilder<TKey>` (`src/Concertable.B2B.KeyedStrategies`), with
`ITenantContactResolver` as its only member. Only `GetContactByTenantIdAsync` is promoted to the module
facades, and after that change nothing in production injects both `IVenueModule` and `IArtistModule`. The
other three reads are deliberately **not** promoted — a facade adapts a use case rather than mirroring a
repository, and no cross-module consumer wants them yet.

**Resolves when:** each new cross-module tenant-keyed need joins the `ITenantStrategy` family as its own
member instead of dual-injecting both facades, and the parallel four-method surface has a declared shape so
a new `TenantType` does not mean a third verbatim copy.

No second member is queued today. The tenant-deletion teardown above is **not** one: it is pub/sub fan-out —
each module handles the integration event for its own rows, mirroring how creation fans out — so it selects
nothing by key. The realistic next member is a cross-module *read* that varies by tenant type, such as a
Tenant-side "has this tenant provisioned its profile yet?" over `ExistsByTenantIdAsync`.

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
the `seeding` skill. The settlement E2E (`ConcertFinishedTests`) reads these via
`TicketsSold * Price`: Past DoorSplit (id 12) and Past Versus (id 9) are seeded `ticketsSold: 1` —
the Versus concert was a real gap the old simulator catalog (concerts 13/12/10) omitted.

---

## LOW

### Action-link hrefs are hand-interpolated instead of generated from routes

Every `ActionLink` in the Api layer builds its href with string interpolation — `ApplicationMapper`,
`ConcertMappers`, `SelfBillingAgreementMappers` and `OpportunityMapper` (all in
`Concert.Api/Mappers/`) plus `Conversations.Api/Mappers/MessageMappers` between them interpolate
roughly a dozen `$"/api/..."` literals. Nothing ties a literal to the controller route it names, so a
route rename leaves the emitted link pointing at a 404 and no build or test fails. The frontend
compensates by stripping the `/api` prefix back off with a regex before re-issuing the call —
`apiPath` in `app/web/b2b/shared/src/features/concerts/api/actionLinkApi.ts` — which only works while
every literal happens to agree.

`Href` (Concertable.Kernel) now validates the string at construction, but validating a string someone
already hand-built is a checkpoint after the fact, not the fix — `LinkGenerator` means nobody builds it.

**Resolves when:** the Api-layer mappers take `LinkGenerator` (or `IUrlHelper`) and produce every
`ActionLink` from a named route rather than an interpolated literal, and the frontend's `apiPath` regex
is deleted because the emitted href is already client-relative.

---

### Admin verification queue enriches contact per row, not per page

`VerificationService.GetPendingAsync` (Tenant.Infrastructure) awaits `IVenueModule`/`IArtistModule`
`GetContactByTenantIdAsync` once per pending row, sequentially (required — see the fixed concurrency bug
this replaced: parallel calls hit the same scoped Venue/ArtistReadDbContext instance). Correct, but O(N)
queries per page instead of one batched lookup per `TenantType` on the page.

**Resolves when:** `IVenueModule`/`IArtistModule` gain a `GetContactsByTenantIdsAsync(IEnumerable<Guid>)`
batch method, and the admin service groups pending rows by `TenantType` and issues one call per group
instead of per row.

---

### `ApplicationController.GetById` branches on `TenantType` to pick a response mapper

`GetById` (Concert.Api) switches on `membership.Type` to choose between `mapper.ToVenueResponse` and
`mapper.ToArtistResponse`, with a `default:` arm returning `Forbid()` — the same "branching on the key
inside a key-agnostic component" anti-pattern the `keyed-strategies` standard names, in a controller
that is otherwise key-agnostic. Found while resolving the Tenant-side equivalent, whose entry asserted
the branch was confined to `VerificationService.GetContactAsync`; it was not.

Deliberately left out of that fix for two reasons. It lives in the **Concert** module, so it needs
Concert's own `TenantType`-keyed spine rather than Tenant's — factories and key enums stay module-local.
And what varies is an Api-layer *response shape* rather than an application-layer value, so the leaves
are response mappers and the `default:` arm is an authorization decision that must survive the refactor
as an explicit check, not silently become a composition-time coverage failure.

**Resolves when:** Concert declares a `TenantType`-keyed family over the shared `KeyedStrategyBuilder<TKey>`
(`src/Concertable.B2B.KeyedStrategies`) with the venue/artist response mappers as its keyed leaves, and
`GetById` selects through it — the not-a-party case staying an explicit authorization check ahead of the
lookup rather than a missing key.

---

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
`VenueHireDeal.HireFee` (contracts + `*DealEntity`) stayed `decimal`. Checkout and the confirm strategies
lift them with `Money.Gbp(deal.Fee)` at the call sites — a legitimate
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

### The `[Admin]` authorization seam is thin, and there is no admin UI for moderation

`AdminAttribute` (`Admin.Api/Authorization`) resolves an `AdminProfileEntity` — a bare `Sub` column with
no roles and no scoping — through `AdminProfileHandler`, which issues an **uncached `AdminDbContext`
query on every request** to every `[Admin]` endpoint. Admin provisioning only happens via registration
through the `admin` client-id (`CredentialRegisteredHandler` calling `IAdminModule.GrantIfEligibleAsync`)
or `AdminTestSeeder`. Until the OSA
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

### `MessageRepository` owns `ThreadReadStateEntity`, which has no repository of its own

`Concertable.B2B.Conversations.Infrastructure/Repositories/MessageRepository.cs:27`, `:46`, `:55` join,
read and `AddAsync` `context.ThreadReadStates`. That is the anti-pattern
the `persistence` skill names in its own heading - "never fold a
satellite entity into another entity's repository" - and the doc cites Conversations as the *precedent*
for the rule it breaks. `Concert/ConcertImageEntity` is the same shape (a `DbSet` with no repository).

Either give each its own repository, or state the exception the rule needs for an owned child collection
that is never queried independently. Do not leave the rule absolute while the code contradicts it.

Resolves when: `grep -n "ThreadReadStates" MessageRepository.cs` returns nothing, or the rule in
`CODE_PATTERNS.md` states the child-collection exception explicitly.

---

### `app/web/shared` still hands B2B a Stripe payment-method id

`StripePaymentForm.onSuccess` is typed `(paymentMethodId: string) => void` and `NewCardSection.onConfirmed`
the same, so the shared web tier offers consumers the `pm_…` id it reads off the confirmed intent. B2B no
longer sends it — apply and accept post only the e-signature, and Payment resolves the method from the
reference — but the seam still exposes it, and a future consumer could pick it back up.

**Why it is not fixed here:** `carve-fe` builds each app against `@concertable/web` **as published to the
feed**, so narrowing the callback in `app/web/shared` and consuming the narrower shape in `app/web/b2b/*`
in one PR fails that gate. Same publish-first split as the `ApplicationActions.decline` entry.

**Resolves when:** `web` is republished with `onSuccess: () => void` / `onConfirmed: () => void`, the b2b
and customer callers drop the argument, and `carve-fe` is green.
