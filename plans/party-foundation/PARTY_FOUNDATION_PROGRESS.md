# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Worktree: `C:/Users/TommySeery/source/repos/Concertable/b2b/.worktrees/Refactor-PartyFoundationLegacyBindings`
- Branch: `Refactor/PartyFoundationLegacyBindings`
- Reviewed base: `309e40d4b4b704fe94246332130566b89f464de4`
- Reviewed implementation head: `189f745d8233b511273624d768bcc39b5508723b`
- Implementation head: `f6ecc9bf` (P1 repair slices 1-3)
- PR: [#18](https://github.com/Concertable/b2b/pull/18).
- Last reconciled: 2026-09-18, Claude Opus P1 implementation, slices 1-3 committed.
- Current authorization: implement, verify and locally commit the P1 feedback and agreed replacement
  mechanisms, including naming corrections, in an independent Claude Opus session.
- Delivery gate: the user's original no-push/no-merge restriction remains; the new request authorizes
  P1 implementation, not PR #18 delivery or P2–P5 execution.
- Future dependency/package gates: recheck overlapping OperationClaim/vocabulary owners; P2
  Concert/Seed/Hosting publication plus Customer/Search consumption; configuration persistence's
  provider choice remains with its owner.

## Current state

P1 implementation is in progress on this branch. Three slices are committed. The unit and architecture tiers
are green, and the Tenant integration tier now runs and passes 80 of 83 — it could not run at all before
slice 3, because the schema did not match the access model. The remaining slices are listed under
`## Next Steps`. P2–P5 remain unimplemented targets.

Plan section 4 remains the implementation contract. Nothing below claims a finding is closed against the
4.10 matrix; the Verification section records exactly what has and has not been run. The working
tree also carries unrelated dirty `CODE_PATTERNS.md` and untracked `.codex/` content, which must be preserved.

## Completed work

**Slice 1 — `5ab4356b` "Compose membership, audience and resource access once" (4.1, 4.2, parts of 4.3/4.4)**

- `MembershipFact` → `MembershipSnapshot(MembershipId, TenantId, UserId, Role, PermissionVersion)`;
  `IMembershipFacts`/`MembershipFacts` → `Authorization.Contracts.IMembershipReadRepository` implemented by
  Tenant's existing `MembershipRepository`, bound to the same scoped instance at Tenant's composition root.
- `ResourceAudience` and `IPermissionCatalog.AudienceFor` added; `RestrictedParticipant` deleted;
  `terms.read`, `bookings.cancel` and `concerts.declare_door_revenue` added as separate operations.
- `AuthorizationVersion` → `PermissionVersion` through entity, configuration, mappers and DTOs.
- `MembershipAuthorityFact` → keyless `MembershipAuthority` mapped `ToView("MembershipAuthority", "tenant")`.
  The view itself is created by slice 3's regenerated Tenant migration.
- `IAccessContext`/`AccessContext`/`IHasAccessContext`/`AccessScopedDbContext`/`DesignTimeAccessContext` →
  the `ResourceAccess*` names; `GrantOrigin` → `ResourceGrantKind` (Principal/SharedSummary/MemberAssignment);
  grant `MemberUserId` → `MembershipId`; base `Revoke` protected, concrete `Revoke` internal.
- `ResourceAccessExpressions` owns the shared membership/validity/time predicate and the body-splicing `And`;
  each of the four resource contexts composes it with its own scope/audience policy. Parent filters now
  reference only the scope that discloses them.
- `ContractEntity` issues its principals' grants in its constructor (F01).
- Concert summary sharing replaces the generic share route: `ShareConcertSummaryRequest`/`ConcertSummaryShare`,
  the four `summary-shares`/`member-assignments` routes, `ConcertEntity.ShareSummary` /
  `RevokeExpiredSummaryShares` / `RevokeSummaryShare` / `AssignMember` / `RemoveMemberAssignment`,
  `AccessVersion`, per-issuer unique indexes, and a `ConcertCommandReceipt` for replay.
  Application and Booking share surfaces are deleted.
- `MessageRepositoryTests` moved to the integration tier (it read through the host stance);
  `Previews_LatestMessageHidden_FallsBackToTheNewestVisibleOne` added there for the one assertion the
  existing integration tests did not already prove.

**Slice 2 — `b2001b2f` "Give system work a named capability instead of an ambient bypass" (4.6)**

- `IExecutionScope`, `IExecutionScopeActivator`, `ExecutionScope`, `ExecutionPurpose` and every host,
  middleware and fixture entry point deleted. `MembershipContext.IsHost` is constantly false.
- `ConcertPrivilegedDbContext`, `ApplicationPrivilegedDbContext`, `BookingPrivilegedDbContext` added beside
  the existing `ConversationsPrivilegedDbContext`; `IConcertPrivilegedRepository`,
  `IInvoicePrivilegedRepository`, `IInvoiceSequenceRepository` and the `PrivilegedRepository<T>` alias.
- Settlement's unit-of-work boundary, `InvoiceIssuer`, all four payment processors and all nine seeders bind
  the privileged stance; seeders keep `MigrateAsync` on the context that owns the migrations.
- The settlement outcome for an unknown target or a mismatched operation now fails durably instead of
  recording a success inbox receipt (F11).
- `GetEndedPendingCompletionIdsAsync` moved to `IConcertReadRepository` with a due-time cutoff and batch;
  `CompletionRunner` consumes it.
- `PublishedConcert` projection with publication in its predicate (F10).

**Slice 3 — `f6ecc9bf` "Make the schema and the seeding path match the access model" (4.10 migrations, 4.6 completion, F16)**

- The five `InitialCreate` migrations are regenerated against the new model: `PermissionVersion`, grant
  `MembershipId` and `Kind`, the per-issuer unique indexes, Concert's `AccessVersion` and its command
  receipts. Tenant's migration creates the `tenant.MembershipAuthority` view the resource filters read
  through; without it every resource read denied and the integration tier could not start.
- Deleting the ambient host stance also disarmed nothing for the *tenant write fence*, which keyed on the
  same flag: every seeder writing a tenant-scoped row for a tenant it is not acting as began to fail.
  Artist, Venue, Opportunity and Deal therefore get the same privileged stance the grant-reached modules
  have. No privileged registration carries `TenantInterceptor` — that stance exists to write across tenants —
  and all of them keep `UseSeedingSupport`, which is a capability a seed explicitly activates.
- `ApplicationPrivilegedDbContext` and `BookingPrivilegedDbContext` were never registered in slice 2; the
  host's strict service-provider validation caught it.
- The three Concert pre-commit domain-event handlers re-read their own aggregate to build an integration
  event, which returned nothing with no human acting; they take the privileged repository.
- `TenantService.DeleteAsync` removes its own business-activity rows before the tenant (F16).
- The tax-compliance round-trip tests read through the suite's JSON options rather than the framework
  default, which cannot parse the business-activity enum.

## Next Steps

Scope: whole plan through all remaining P1 phases; delivery stays gated.
Current slice: 4.5 — one local transaction per command.
Remaining scope: slices 1-6 below, then the retained PR #18 delivery gate, then P2–P5.
Done when: F01–F28 are repaired, verified against plan section 4.10 and reviewed; changes are locally
committed and this ledger records the actual results and the remaining delivery gate.

Continue on this branch, in this order. Each item is a slice: implement, build, run the unit and architecture
tiers, commit.

1. **4.5 — one local transaction per command.** `CommandTransaction`/`CommandTransactionAccessor`, enlistment
   of every participating context, `FlushAsync`/`ValidateAuthorityAsync`/`CommitAsync`, the execution-strategy
   loop and the lock order. Delete `SharedConnectionExtensions` and give ordinary and parallel dashboard reads
   their own connections. Move `InvoiceIssuer` onto its repositories once the enlisted context exists.
2. **4.3 — command policy on every mutation.** The fenced actor, the exact grant/audience check per command,
   the command table's business actor per operation, `ApplicationSide` deleted, actions computed per
   operation, and the share/assignment commands moved onto the fence and lock order from slice 1.
3. **4.2 completion — exact-scope reads.** `ConcertSummary` without financial fields, `ApplicationSummary`
   without proposal text, separate Summary/Operations/Terms/Finance routes and their permission checks,
   removal of generic private-detail endpoints, and the financial dashboard split (F20).
4. **4.7 — Conversation.** The whole `Thread` → `Conversation` rename to the grep gate, `ConversationId`-addressed
   creation and send with request receipts, immutable initial audience, message sequence, monotonic member
   read position, the Tenant-owned display projection, and safe delivery.
5. **4.8/4.9 — neutral business lifecycle and clients.** `TenantBusinessActivity`, neutral onboarding,
   contact/activity administration, invitation role policy, tenant deletion, the admin verification contract,
   and the real Business web and mobile journeys with tenant-switch isolation.
6. **4.10 — qualification.** Replace `ResourceAccessGuardTests`'s textual filter check with model and
   provider coverage, and run the full 4.10 matrix: every module's integration tier, the provider-level race
   and revocation-ordering cases, workers, contract/invoice reads, external-summary denial, and real browser
   and native evidence. Then review the candidate. The migrations and the authority view are already done.

## Decisions and findings

- Keep Tenant as business/legal/membership/settlement identity and tokens as sub/email only.
- Keep multiple business activities, module-local resource grants and server-returned permissions;
  eligibility, permission and resource audience have separate, explicit jobs.
- Reject ambient ExecutionPurpose/IsHost bypass, any-scope private details and unchecked load/save.
- P1 external disclosure is Concert Summary only. Operational member assignments stay inside a
  current principal business; accepted external responsibilities wait for P2.
- Keep one local database transaction for a command; give ordinary/parallel reads separate connections.
- Corrected handoff premise: platform 0.2.0-alpha.0.5 / source 3136a4ee writes outbox through the active
  business DbContext. Its dispatcher connection is not a second ordinary business write.
- Keep the branch's economic direction properties. P2 replaces their fixed principal inputs with
  accepted payer/payee bindings; InvoiceParty remains reserved for the invoice snapshot.
- Do not adopt Finbuckle as a P1 repair: its single-owner tenancy mechanism does not replace the
  many-tenant scope/permission/fence policy. A future adoption needs an independent measured benefit.
- Do not inflate P1 with configurable roles/hierarchy. The separately owned follow-on must preserve
  the P1 permission/audience contract and prove administration/revocation across all consumers.
- New naming and exact before/after mechanisms are in plan section 4; do not re-invent them from this ledger.
- Membership snapshot queries belong to the existing MembershipRepository through Authorization's narrow
  IMembershipReadRepository contract.
- No production compatibility layer, old schema reader, adapter or synthetic-data backfill is needed.
- **From implementation:** the authority relation is exposed as a `DbSet<MembershipAuthority>`, not an
  `IQueryable`. Only a DbSet is a query root; an IQueryable member is parameterised whole and its `Any` then
  fails to translate. Its keylessness, not its member type, is what makes it unwritable.
- **From implementation:** `ContractAccessScope.Terms` and `InvoiceAccessScope.Invoice` are renamed to
  `Read`, matching the scope names plan section 4.2's table and mandated code use.
- **From implementation:** `InvoiceSequenceEntity` is keyed by its issuing tenant and implements no id-entity
  contract, so `IInvoiceSequenceRepository` takes no generic CRUD base.
- **From implementation:** a seeder keeps `MigrateAsync` on the filtered context, which owns the module's
  migrations, while its reads and writes use the privileged one. Pointing both at the privileged context
  would leave it applying migrations it does not own.
- **From implementation:** the platform's `TenantInterceptor` is the single-owner *write* fence and keys on
  the same `IsHost` the read filters did. Deleting the ambient stance therefore breaks every cross-tenant
  write, not just cross-tenant reads — which is why Artist, Venue, Opportunity and Deal needed a privileged
  stance even though they own no resource grants. A privileged registration must omit that interceptor and
  keep `UseSeedingSupport`.
- **From implementation:** a pre-commit domain-event handler that re-reads its own aggregate is system work
  and must use the privileged repository. Reading through the shared request connection works: the seeding
  dispatcher runs pre-commit handlers after the save.

## Verification

At `f6ecc9bf`:

- Solution build: clean (2 pre-existing `CS8632` warnings in the UI E2E project).
- Unit and architecture tiers: **0 failed** across 14 assemblies.
- Tenant integration tier: **80 passed, 3 failed**. The three are
  `InvitationTests.Invite_AsOwner_CreatesPendingInvitationAndSendsEmail`,
  `InvitationTests.Invite_AsArtistOwner_SendsEmailWithArtistPortalAcceptLink` and
  `VerificationAdminApiTests.Approve_ShouldReturn204_AndSendNothing_WhenTenantOwnsNoProfile`. Both causes are
  named P1 findings owned by the business-lifecycle slice: the invitation email's neutral acceptance route
  (F19) and the verification notice for a tenant holding no marketplace profile (F14). Neither handler is
  touched by any of these three slices — confirmed with `git diff --name-only` against the pre-implementation
  head — so they are pre-existing branch behaviour that this tier simply could not reach before.

Not yet run or produced, and required by plan section 4.10 before any completion claim:

- Every other module's integration tier (Application, Booking, Concert, Conversations, Venue, Artist, Admin,
  Lifecycle, Process). Only Tenant's has been run.
- `StartupTests.ResourceGraphTests.ProductionGraphAndStrictValidation_AreValid` fails in this environment
  waiting on the Stripe CLI resource (`Concertable.Payment.Hosting.AppHostExtensions.AddStripeCli` times
  out). Environmental and unrelated to these changes; re-check where the CLI is available.
- Provider-level race, revocation-ordering, worker, contract/invoice read, external-summary denial, browser
  and native evidence. None has been produced.

The naming audit uses the installed C# naming, persistence and multitenancy standards.

## Reviews

Current review: source review and P1 re-specification, with a bounded authority sanity check. F01–F28 remain
implementation findings owned by plan section 4; no runtime approval is granted. **The three committed slices
have not been reviewed.** The [existing review artifact](../../reviews/Refactor-PartyFoundationLegacyBindings.md)
records earlier candidates and is not a completed canonical review of this checkpoint.

## External owners and deferred work

Automatic standards refresh for Codex/Claude and generic naming publication are owned by the separate
Codex tab `Standards refresh and naming`. This Claude handoff owns the P1 application changes;
follow the plan's agreed naming inventory without taking over that tooling work.

The dependency/debt tables in plan sections 10–11 remain the owning map for future delivery.
Their sibling PR/head observations are dated 15 September and must be refreshed before implementation;
do not treat them as live merge-state assertions.

Central docs remains the owner of configurable-workflow product authority, configuration expressiveness,
nightclub benchmark and launch roadmaps. At the next authorized implementation/documentation boundary,
send its owner the actual qualified P1–P5 artifact and remove stale adapter/backfill/old-worktree claims.
No sibling checkout was edited by this task.

Tenant retirement/account teardown, authoritative sales evidence, payment quote disclosure,
transport qualification, richer admin roles and configurable RBAC retain their explicit owners and
completion gates. None is silently declared fixed by this resource-access foundation.
