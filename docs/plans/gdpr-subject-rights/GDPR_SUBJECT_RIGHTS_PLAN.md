# GDPR Subject Rights — Erasure + Data Export Plan

**Next steps live in @docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS_PROGRESS.md → `## Next Steps`**

## Context

UK GDPR gives a data subject the right to **erasure** (art. 17) and to a **portable copy** of their data
(art. 15 access / art. 20 portability). Concertable has neither: the code sweep and a full enumeration of
the subject surface confirm there is **no account deletion, anonymisation, export, soft-delete or retention
machinery anywhere in `api/`** — `IAuditable` (Payment `TransactionEntity`/`EscrowEntity` only) is the sole
adjacent concept; no `ISoftDelete`/`IsDeleted`, no `Anonymise`/`Erase`/`Export`/`Retention` type exists. This
is the regulator obligation that [`../../../api/src/Modules/Deal/LEGAL_REQUIREMENTS.md`](../../../api/src/Modules/Deal/LEGAL_REQUIREMENTS.md)
item 8 ("GDPR data retention + right to erasure — ABSENT") and Phase 2 of
[`LAUNCH_CHECKLIST.md`](LAUNCH_CHECKLIST.md) ("DSAR process documented", "Data retention schedule
documented") have been holding open.

**This is not a `DELETE`.** The financial record is HMRC-retained for **six years** (self-billed VAT
invoices, self-billing agreements, ledger entries), and signed contracts + their e-signatures are
contract-law evidence for the limitation period. A cascade delete would destroy the statutory records the
platform is *required* to keep. So the capability is a **designed retain-vs-erase split**: anonymise the
natural person (name, email, contact, credentials, tokens), **retain the financial/legal record with the
personal identity severed or, where the record itself is the evidence, retained intact**.

The subject surface spans four services and must be handled **only** through each service's own facade and
integration events — never a cross-service query (the load-bearing rule in
[`../../../ARCHITECTURE.md`](../../../ARCHITECTURE.md)). The building blocks already exist and are named in
the design below; nothing here invents new infrastructure.

### The subject, and who owns erasure/export

A **data subject is a natural person**, identified by the Auth credential `sub` (`Guid Id` on
`CredentialEntity`) — the root identity every service keys users by. A person is either a **B2B subject** (a
venue/artist manager) or a **Customer subject** (a fan); B2B and Customer are two separate systems (per both
`LEGAL_REQUIREMENTS.md` docs), so a person present in both is two subject records, one per system.

Consequently **erasure and export are owned by the data service the subject belongs to** (B2B or Customer).
Within that service the work runs through in-process module facades (`I*Module`); it reaches the **adapter**
services synchronously (Auth to tombstone the credential, Payment over gRPC to scrub its one PII field and to
report financial obligations) — a data→adapter call, which the boundary rules permit. Cross-*data*-service
reach and read-model projections are handled by the Auth-published erasure event fan-out, never a direct
data→data call. A `Tenant` is a **legal entity, not a person**: erasing a human severs their membership and
anonymises their personal rows; the tenant's settlement identity persists under the retain-vs-erase table
below.

## The retain-vs-erase decision table (the spine — exhaustive)

Every row is a call the solicitor must confirm; the legal basis is stated so they can. Decisions:
**ERASE** = anonymise/scrub in place; **SEVER** = keep the record, pseudonymise or drop the identity link,
keep the non-personal (financial) fields; **RETAIN** = keep the row intact for the statutory window because
the row *is* the evidence; **PURGE** = hard-delete (transient, no retention need). `[LEGAL]` marks a call
whose basis needs solicitor ratification before merge.

### Auth (`api/Concertable.Auth`) — identity root

| Store · field | Decision | Basis |
|---|---|---|
| `CredentialEntity.Email` | ERASE → stable tombstone pseudonym (keep the `sub` row so downstream FKs stay valid) | identity; no retention need |
| `CredentialEntity.PasswordHash`, `IsEmailVerified` | ERASE (scrub) | secret |
| `EmailVerificationTokenEntity`, `PasswordResetTokenEntity` | PURGE | transient |
| Duende persisted grants / refresh tokens (`PersistedGrantDbContext`) | PURGE (revoke) | session |

### B2B (`api/Concertable.B2B`)

| Store · field | Decision | Basis |
|---|---|---|
| `Modules/User` `UserEntity` — `Email`, `Address`, `Location`, `Avatar` | ERASE | no statutory need |
| `Modules/Tenant` `TenantMembershipEntity` | PURGE (sever), guarded by the **last-owner invariant** (mirror `MembershipService.IsLastOwnerAsync`) | membership link |
| `Modules/Tenant` `TenantInvitationEntity.Email` | PURGE pending; accepted rows already severed via membership | transient |
| `Modules/Tenant` `TenantEntity.LegalName` + `TaxCompliance` (`SellerIdentifier` [NINO/UTR], `VatNumber`, `BankReference`, `RegisteredAddress`) | RETAIN while the tenant has other members or settled financial history in the 6-year window (it is a distinct legal entity); when the erased person is the **last member** of a sole-trader tenant, SEVER the membership but RETAIN the tax identity for the HMRC six-year window, flagged pending-purge, purged by the sweep after the window | HMRC six-year retention; agent VAT posture `[LEGAL]` |
| `Modules/Concert` `InvoiceEntity` + `InvoiceParty` (LegalName, VatNumber, address) | RETAIN intact | HMRC VAT Act invoice-retention (item 8 states this explicitly) |
| `Modules/Concert` `SelfBillingAgreementEntity` (`InvoiceParty` + `ESignature`) | RETAIN intact | HMRC self-billing agreement |
| `Modules/Concert` `ContractEntity` (VenueName, ArtistName, both `ESignature`) | RETAIN intact for the contract-limitation period | contract-law / dispute evidence (item 2) |
| `Modules/Concert` `ESignature` VO (`SignatoryName`, `Ip`, `UserAgent`, `DrawnSignatureImage`) | RETAIN intact — the signature **is** the consent evidence; erasing it destroys the record it exists to preserve | ECA 2000 / eIDAS `[LEGAL]` |
| `Modules/Conversations` `MessageEntity.Content` (+ `SenderTenantId`/`SentByUserId`) | SEVER — keep the body, drop the personal identity link — for the limitation/OSA window, then PURGE by the sweep `[LEGAL]` (retain-vs-erase call: booking-dispute + OSA evidence vs minimisation) | dispute evidence / OSA |
| `Modules/Conversations` `ContentReportEntity` (`MessageExcerpt`, ids, notes) | RETAIN while the OSA record obligation is live | OSA evidence |
| `Modules/Conversations` `ParticipantProfile` read model (Name, Address) | ERASE (re-project to pseudonym) | projection copy |

### Customer (`api/Concertable.Customer`)

| Store · field | Decision | Basis |
|---|---|---|
| `Modules/User` `UserEntity` — `Email`, `Location`, `Address` | ERASE | no statutory need |
| `Modules/Ticket` `TicketEntity` (`UserId`, `QrCode`, denormalised names, `Price`) | SEVER — drop the `UserId` link, keep the financial proof-of-purchase fields; scrub `QrCode` post-event | consumer-rights / financial record |
| `Modules/Review` `ReviewEntity` (**keyed by `Email`**, `Details`) | ERASE — scrub `Email` + free-text `Details`; keep the anonymous `Stars` aggregate | no statutory need; DMCCA genuine-review nuance `[LEGAL]` |
| `Modules/Preference` `PreferenceEntity` (`UserId`) | PURGE | no need |

### Payment (`api/Concertable.Payment`) — opaque-owner keyed, financial

| Store · field | Decision | Basis |
|---|---|---|
| `PayoutAccountEntity.Email` (+ `StripeAccountId`/`StripeCustomerId`) | ERASE (scrub `Email`); detach the Stripe customer; keep the row for ledger linkage | only direct PII in Payment |
| `TransactionEntity`/`LedgerEntryEntity`/`LedgerAccountEntity`/`EscrowEntity` (opaque `OwnerId`/`PayerId`/`PayeeId`, amounts, `IAuditable` stamps) | RETAIN intact — no names; opaque keys only | HMRC six-year / double-entry integrity |
| `StripeEventEntity` (raw webhook payload) | PURGE after the idempotency window (may embed PII) `[LEGAL]` | webhook idempotency only |

### Search (`api/Concertable.Search`)

| Store | Decision | Basis |
|---|---|---|
| Projections holding copied artist/venue names + emails | ERASE — re-project to pseudonym driven by the erasure event it already consumes | projection copy |

## Design decisions

### 1. Erasure is an orchestrated, gated state machine — never a cascade delete

A new **`SubjectErasureRequest`** aggregate in the owning data service carries the request through a small
state machine: `Requested → Deferred → InProgress → Completed` (plus `Failed` for a hard error). It is the
durable record that a DSAR erasure was raised, gated, and executed — the evidence an ICO enquiry would ask
for. The request is idempotent and re-drivable, so a `Deferred` request costs nothing to retry.

### 2. Fail-closed erasure gate + self-healing sweep — mirror the DAC7/self-billing pattern

Erasing a subject with **live financial obligations** (an unsettled/booked concert, held escrow, a pending
payout, an un-expired `SelfBillingAgreementEntity`) must **defer**, not proceed — anonymising identity
mid-settlement would break settlement and the audit trail. This reuses the shipped fail-closed pattern
verbatim in shape:

- The gate mirrors `FinishExecutor`'s two-gate check
  (`Modules/Concert/Concertable.B2B.Concert.Infrastructure/Services/Workflow/Executors/FinishExecutor.cs`):
  each check returns a `Deferred…` outcome rather than throwing, and the request stays `Deferred`.
- A **self-healing hourly sweep** mirrors `ConcertFinishedFunction`
  (`src/Concertable.B2B.Workers/Functions/ConcertFinishedFunction.cs`, `[TimerTrigger]` hourly) →
  `IConcertCompletionRunner`: an erasure runner re-drives every `Deferred` request idempotently, so the
  moment the last obligation settles the erasure completes without a human re-poking it.
- **Fail-closed means fail-closed:** an obligation the owning service cannot yet see (Payment-side escrow
  before Phase 4 wires it) defers rather than erases. Each phase *tightens* what the gate can see; it never
  loosens.

### 3. Cross-service orchestration is the existing provisioning fan-out, run in reverse

The subject was **created** by an Auth `CredentialRegisteredEvent` fanned out to
`Modules/User/CredentialRegisteredHandler`, `Modules/Tenant/TenantProvisioningHandler` (B2B),
`UserCreationHandler` (Customer) and `CustomerRegisteredHandler` (Payment) — inbox-idempotent handlers
filtered by `ClientId`. Erasure adds the mirror:

- A new **`CredentialErasedEvent`** in `Concertable.Auth.Contracts`, wire identity
  `[MessageType("concertable.auth.credential-erased.v1")]`, carrying the `sub` (and normalised email for the
  email-keyed stores). Auth publishes it through the same `IBus`/`OutboxBus` transactional-outbox path after
  it tombstones the credential.
- Each service subscribes with an inbox-idempotent `IIntegrationEventHandler<CredentialErasedEvent>`
  mirroring the provisioning handler shape, and scrubs **its own** projections/identity copies (Search, the
  *other* data service if the person exists there, B2B/Customer read models). Topology is declared with
  `.Publish<CredentialErasedEvent>()` / `.Subscribe<CredentialErasedEvent>(service)` in the per-service
  `*Topology.cs`.
- This keeps every cross-service edge an event or a data→adapter call. **No published contract shape
  changes** — the event is *new*, facade members are *added*, the Payment gRPC method is *new* — so every
  cross-service delivery step is additive/non-breaking. If implementation nonetheless finds a published
  `Concertable.*` contract whose shape must change, that is a breaking package change and gets its own
  expand/contract plan (the `plans` skill rule); it is not forced into a feature PR here.

### 4. Export is assembled in-process by the owning service, reaching adapters synchronously

The owning data service assembles the subject's **portable export** — a structured **JSON bundle** (ICO
portability guidance: machine-readable, one document, per-module sections) — from its own module facades
in-process, plus the **Auth identity fragment** and the **Payment financial fragment** (gRPC — Payment is
already the one gRPC surface, `AddPaymentClient`). No data→data call; no cross-service query. The export is a
read; erasure is the write fan-out — two separate flows sharing the same facades.

### 5. A reachable operator route now; the admin console is the eventual home

Per the MVP scope, this ships the **capability + a reachable admin-gated route**
(`POST …/subject-erasure`, `GET …/subject-export`), driven and proven by integration tests exactly as the
admin-provisioning backend was proven before its SPA existed. The polished operator UI is the natural tenant
of the admin console (`launch/admin-console`) when it lands; wiring it there is out of scope and **not** an
implementation dependency — the backend is fully testable without it. A self-service "delete my account"
consumer flow beyond the required route is out of scope (§ Non-goals).

**Consumption contract — fixed here, the consuming UI deferred.** The output shape both routes hand back is
pinned now, even though the console that calls them lands later; a producer with an undecided output is not a
shippable phase:

- **Erasure** — `POST /api/subject-erasure/{subjectId}` returns the `SubjectErasureRequestDto` state
  (`Completed`, or `Deferred` + `DeferralReason`) as **inline JSON**, synchronous and re-drivable; the panel
  renders the state and the deferral reason.
- **Export** — `GET /api/subject-export/{subjectId}` returns a **downloadable JSON file** (`FileDownload` →
  `File(...)` with `Content-Disposition`), synchronous, matching the platform's existing PDF-download
  convention so the console reuses the shared `arraybuffer → blob` download hook instead of re-inventing it.
  It is **not** inline JSON, and there is **no** materialised export DTO — nothing consumes it as a typed
  object, so the module fragments are composed straight into the serialised file (only the per-module
  fragments are typed).
- The console's **erasure-queue view** needs a paginated `GET` list of erasure requests, which does **not**
  exist yet (only the `POST` does). That endpoint is genuinely new backend, delivered with the DSAR panel —
  `launch/admin-console` Phase 5, not this plan.

### 6. The durable compliance record outlives this plan

The ratified retain-vs-erase table + the **DSAR response SLA** (UK GDPR statutory: **one calendar month**,
extendable by two further months for complex/numerous requests) live in a new standing
**`docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS.md`** compliance doc — a bare-stem reference doc mirroring
[`OSA_COMPLIANCE.md`](OSA_COMPLIANCE.md) (risk/decision tables, `[LEGAL]`/`[DECIDE]` markers, a sign-off
checklist) — and item 8 of B2B `LEGAL_REQUIREMENTS.md` is updated from ABSENT to the shipped design. That
doc, not this plan, is the solicitor's review surface and the record that survives when this plan is deleted
on ship.

## Phases

Each phase builds, tests, and ships independently and ends green. Cross-service delivery spans several PRs,
sequenced producer-first; each step is additive (§ design decision 3), so the codebase stays in sync at
every phase boundary. The model change in every phase ends with `./initial-migrations.ps1` (re-scaffold,
never additive migrations). The merge-queue owns the E2E gate; no phase runs E2E locally.

### Phase 1 — Decision register + B2B-local erasure & export behind the gate

- The standing `docs/plans/gdpr-subject-rights/GDPR_SUBJECT_RIGHTS.md` compliance doc (the table above + DSAR SLA + `[LEGAL]`
  sign-off checklist) and the `LEGAL_REQUIREMENTS.md` item-8 update.
- `SubjectErasureRequest` aggregate + state machine + the fail-closed **erasure gate** abstraction, gating on
  what B2B can see today (un-expired `SelfBillingAgreementEntity` via the existing `ISelfBillingAgreementGate`
  shape; unsettled/`Booked` concerts). Payment-side obligations are added in Phase 4 (gate stays fail-closed
  meanwhile).
- Per-module **erasure facade** additions (`IUserModule`, `ITenantModule`, `IConcertModule`,
  `IConversationsModule`) applying the table's B2B rows: ERASE `UserEntity`; SEVER membership under the
  last-owner invariant; scrub `ParticipantProfile`; SEVER `MessageEntity.Content`; **leave every RETAIN row
  untouched** (invoices, contracts, self-billing, `ESignature`). Export facade additions producing each
  module's JSON fragment.
- Reachable admin-gated `POST /api/…/subject-erasure` + `GET /api/…/subject-export`; the JSON export bundle
  assembled from B2B module fragments (Auth + Payment fragments land in Phases 2/4).
- **Verification gate:** unit tests for the state machine, the gate's defer outcomes, and the last-owner
  invariant; integration tests proving (a) a clean subject's B2B rows are anonymised while **every** RETAIN
  row is byte-for-byte unchanged, (b) a subject with a live obligation **defers**, (c) the downloaded export
  file contains exactly the subject's B2B data. `./initial-migrations.ps1`; `plan_graph.py`.

### Phase 2 — Auth credential erasure + the fan-out

- `CredentialErasedEvent` (`Concertable.Auth.Contracts`) + the Auth credential-tombstone capability
  (anonymise `CredentialEntity`, PURGE tokens, revoke Duende grants) publishing through `OutboxBus`; topology
  `.Publish<…>()`.
- B2B commands Auth to tombstone as the final step of a completed erasure (data→adapter); B2B + Search
  subscribe to `CredentialErasedEvent` to scrub identity copies / re-project.
- **Verification gate:** Auth unit + integration (tombstone + publish); an integration test asserting the
  Search/read-model projection is re-projected to the pseudonym on the event. `./initial-migrations.ps1`.

### Phase 3 — Customer subject erasure + export

- Mirror Phase 1 for Customer modules: ERASE `UserEntity`; SEVER `TicketEntity` (drop `UserId`, keep
  financial fields, scrub `QrCode` post-event); ERASE `ReviewEntity` **matched by `Email`** (the review has
  no `UserId`) + scrub `Details`; PURGE `PreferenceEntity`. Customer subscribes to `CredentialErasedEvent`.
- Reachable Customer admin-gated erasure/export routes + Customer export fragment.
- **Verification gate:** integration tests including the **email-keyed** review match (the row a `sub`-only
  sweep would miss) and the ticket sever-not-delete. `./initial-migrations.ps1`.

### Phase 4 — Payment fragment + gate tightening

- New Payment gRPC method(s) (`payment.proto` + `AddPaymentClient` stub): scrub `PayoutAccountEntity.Email` +
  detach the Stripe customer; report a subject's **open financial obligations** (held escrow, pending
  payout); produce the Payment **export fragment** (transactions/ledger scoped to the opaque owner). RETAIN
  the ledger intact.
- B2B + Customer erasure now command Payment (scrub) and feed Payment obligations into the fail-closed gate;
  export bundles include the Payment fragment.
- **Verification gate:** Payment unit + integration (email scrub, obligation report, export fragment); an
  integration test proving the gate **defers** while Payment reports a held escrow. `./initial-migrations.ps1`.

### Phase 5 — Deferred-erasure sweep end-to-end

- The hourly erasure sweep (mirror `ConcertFinishedFunction` → an `ISubjectErasureRunner`) re-drives every
  `Deferred` request idempotently; the sole-trader-tenant **pending-purge** window purge runs here.
- **Verification gate:** integration test — a deferred erasure completes automatically once its last
  obligation settles, and a still-obligated one stays deferred; idempotent re-run asserts exactly-once
  anonymisation. `./initial-migrations.ps1`.

## Non-goals

- Customer-marketplace-only fan concerns beyond the shared `User`/erasure surface — the marketplace is
  deferred (`MARKETPLACE_PLAN.md`); its fan-PII leads live in `Concertable.Customer/LEGAL_REQUIREMENTS.md` §E.
- The DAC7 annual export script (separate roadmap item; first run Jan 2028).
- A designed consumer-facing self-service "delete my account" UI beyond the required reachable route.
- The admin-console operator UI for driving DSARs (`launch/admin-console`'s tenant when it lands).
- A new secret/config store — none is introduced; config reads stay ordinary `IConfiguration`.
- Changing any **published** `Concertable.*` contract shape — every cross-service addition here is additive;
  a breaking change, if discovered, is captured in its own expand/contract plan, not forced in.
