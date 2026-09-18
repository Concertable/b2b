# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Branch: `Refactor/PartyFoundationLegacyBindings`
- Reviewed base: `309e40d4b4b704fe94246332130566b89f464de4`
- Prior implementation head: `f6ecc9bf` (P1 slices 1-3)
- Current checkpoint: P1 4.3 Application and Booking command policy implemented and locally verified
- PR: [#18](https://github.com/Concertable/b2b/pull/18)
- Delivery gate: do not push or merge; the user authorized local P1 implementation and commits only.

## Current state

P1 implementation is active on this branch. Slices 1-4 and the Application and Booking portions of 4.3 are implemented.
Application apply, accept, reject, cancel and withdraw now execute through the root command transaction with
locked current-membership authority, exact Proposal grants, privileged mutation repositories and a final
post-flush authority check. Payment verification and its Booking handlers join the same root transaction,
and module-owned artist, opportunity, deal and venue command facts enlist their privileged contexts.
Booking cancellation now admits either accepted principal under `bookings.cancel` and the exact Operations
grant, using the same locked membership/resource/final-validation sequence.

Provider-real Application transition races now serialize to one successful transition and one conflict. The
cross-module rollback probe reaches the Booking failure and proves that Application, Booking, Concert and
outbound-message changes roll back together.

## Completed slices

### 1 — Compose membership, audience and resource access once

Commit `5ab4356b` introduced `MembershipSnapshot`, resource audiences, operation-specific permissions,
module-local access scopes and grants, exact resource filters, contract principal grants, Concert summary
sharing/member assignment, `AccessVersion`, and Concert command receipts. Generic Application/Booking share
surfaces and the binary restricted-participant fallback were removed.

### 2 — Give system work a named capability

Commit `b2001b2f` deleted the ambient host/execution-purpose bypass. Privileged contexts and repositories now
serve explicit system workflows and seeders. Completion discovery moved to a read repository, published
Concert projection predicates include publication state, and mismatched settlement outcomes no longer mint
successful inbox receipts.

### 3 — Align schema and seeding with the access model

Commit `f6ecc9bf` regenerated the five initial migrations for the access model, including the Tenant authority
view, permission/access versions, membership grant keys, grant uniqueness and command receipts. Privileged
seeding stances omit the tenant write interceptor, pre-commit Concert handlers use privileged reads, and
Tenant deletion removes owned activity rows first.

### 4 — One local transaction per command

- Deleted `SharedConnectionExtensions`; every ordinary module/read context now uses its own connection string.
- Added `CommandTransaction`, a scoped accessor, per-module enlistment behaviors and one root command executor.
- The root executor allocates a fresh DI scope, handler graph, SQL connection and transaction for every
  execution-strategy attempt; the transitional same-scope boundary no longer performs unsafe retries.
- Every participating EF context explicitly joins the owned SQL transaction. Result failures poison the root
  and roll back even when no exception is thrown.
- Flush iterates every enlisted context through its normal `SaveChangesAsync` event/outbox pipeline until the
  graph is quiescent, then runs registered authority validators and commits.
- EF transaction wrappers and scoped contexts are disposed before the owned connection.
- `CommandOutcome` recognizes all Reunion result families used by the service.
- `InvoiceIssuer` uses enlisted privileged invoice and sequence repositories. Invoice sequence and Concert
  mutation reads acquire explicit `UPDLOCK, HOLDLOCK` key locks.
- Settlement reserve and completion run in separate fresh command scopes around the external provider call.
- Settlement outcome processors now keep inbox evidence, Concert mutation, invoice/activity work and outbox
  insertion inside one privileged transaction; unknown targets and operations fail without a receipt.

### 4.3 — Application command policy

- Added enlisted command-fact ports for Artist, Opportunity, Deal and Venue so Application commands do not
  call ordinary module facades inside the root transaction.
- Apply, accept, reject, cancel and withdraw fence current membership, require their exact permission and
  Proposal grant, lock Application resources and grants, and revalidate authority after the final flush.
- The command executor can return a typed closed failure when final authority is no longer valid, rolling the
  entire transaction back before commit.
- Payment verification uses the privileged Application stance, while Booking verification handlers resolve
  and mutate through their privileged repository and workflow in the same command transaction.
- Save-interceptor race simulations were replaced with concurrent HTTP requests against the real provider.
- Booking cancellation uses the exact `bookings.cancel` API permission, admits either accepted principal,
  locks Booking and Operations grants, and revalidates authority after flush.
- Booking payment verification uses the same resource lock as cancellation, so confirmation and cancellation
  serialize without optimistic-concurrency leakage.
- Booking save-interceptor races were replaced with provider-real concurrent cancellations and confirmation
  outcomes.

## Next steps

Continue in this order. Each slice ends with a solution build, unit/architecture gate and local commit.

1. **4.3 — finish command policy on every mutation.** Migrate Concert edit/post/door
   revenue/check-in/cancel, Concert summary sharing and member assignment. Delete
   `ApplicationSide`; compute actions per exact operation.
2. **4.2 completion — exact-scope reads.** Remove financial/proposal fields from summaries, split Summary,
   Operations, Terms and Finance routes and permission checks, delete generic private-detail endpoints, and
   separate financial dashboards.
3. **4.7 — Conversation.** Complete `Thread` → `Conversation`, address create/send by `ConversationId`, add
   request receipts, immutable initial audience, message sequence, monotonic read position, Tenant display
   projections and safe delivery.
4. **4.8/4.9 — neutral lifecycle and clients.** Finish `TenantBusinessActivity`, neutral onboarding,
   contact/activity administration, invitation role policy, deletion and admin verification contracts, then
   implement the real Business web/mobile journeys and tenant-switch isolation.
5. **4.10 — qualification.** Replace textual resource-filter tests with model/provider coverage and run every
   module integration tier, provider race/revocation cases, workers, contract/invoice access, external summary
   denial, browser and native evidence. Run the canonical review over the completed P1 candidate.

## Verification at the current checkpoint

- `dotnet build Concertable.B2B.slnx --no-restore`: passed, 0 errors; three existing warnings.
- DataAccess Unit tier: 5 passed.
- Architecture tier: 24 passed. The new DataAccess tests now carry the Unit assembly trait and a direct
  Reunion reference, satisfying CI ownership checks.
- Concert unit tier: 96 passed.
- `ConcertDoorSplitApiTests`: 8 passed through fresh reserve/complete command scopes, covering provider
  operation reuse, persistence interruption, duplicate outcomes and invoice completion.
- Application integration tier: 74 passed, including provider-real accept/reject, accept/cancel,
  accept/withdraw and competing-acceptance races.
- Booking integration tier: 24 passed, including both-principal cancellation, outsider denial, duplicate
  cancellation, capture/cancellation and payment-verification/cancellation races.
- Cross-module rollback probe
  `CaptureSuccess_WhenBookingSaveFails_RollsBackBookingConcertAndOutboundMessages`: passed.

Still required before a P1 completion claim: every module integration tier, provider race and revocation
ordering coverage, workers, contract/invoice reads, external-summary denial, real browser/native evidence and
canonical review. The Startup resource-graph case previously timed out waiting for the Stripe CLI and must be
rechecked where that resource is available.

## Stable decisions

- Tenant remains the business/legal/membership/settlement identity; tokens remain identity-only.
- Eligibility, permission and resource audience are separate facts.
- No ambient host bypass, query-filter bypass, generic private-details endpoint or unchecked load/save.
- P1 external disclosure is Concert Summary only; operational member assignments stay inside a principal
  tenant. Accepted third-business responsibilities remain P2.
- Protected commands use one local transaction. Ordinary and parallel reads use independent connections.
- Interactive command retries use a fresh scope and durable receipt; external payment/blob calls remain
  outside the database transaction and reuse a durable provider operation identity.
- No compatibility layer or data backfill: the product is pre-launch.
- Migrations stay owned by the filtered context; privileged contexts perform explicit cross-tenant system work.

## External/deferred owners

P2-P5, dependency publication, configurable-workflow documentation, configurable RBAC, tenant retirement,
sales evidence, payment quote disclosure and transport qualification retain their existing plan owners and
gates. No sibling checkout is modified by this P1 run.
