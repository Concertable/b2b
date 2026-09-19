# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Worktree: `C:\Users\TommySeery\source\repos\Concertable\b2b\.worktrees\Refactor-PartyFoundationLegacyBindings`
- Branch: `Refactor/PartyFoundationLegacyBindings`
- PR: [#18](https://github.com/Concertable/b2b/pull/18)
- Reviewed base: `309e40d4b4b704fe94246332130566b89f464de4`
- Current checkpoint: P1 4.8 backend candidate awaiting current-main reconciliation
- Dependency/package gates: reconcile the long-lived branch with the PostgreSQL default branch before qualification
- Delivery gate: authorized through canonical review, commit, push, exact-head remote validation and merge
- Last reconciled: 19 September 2026 against HEAD `6d466a87`, `origin/main` `7fd22b46` and the current worktree

## Current state

P1 slices 1-4, 4.3, 4.1 and 4.7 are implemented. The 4.8 authenticated Business web/mobile cutover is
committed at `6d466a87`; the matching backend Tenant lifecycle and legacy-binding removal are an uncommitted
candidate. The default branch is fourteen commits ahead and has replaced B2B SQL Server composition with
PostgreSQL. This branch must absorb that provider graph before its backend integration evidence is meaningful.

The Tenant initial migration retains its hand-authored `tenant.MembershipAuthority` view after regeneration.
Conversation read-position writes use a dedicated repository and an atomic range-locked maximum update.
Shared, Artist and Venue clients consume the conversation API and close/reopen notifications across tenant
switches. Preserve the unrelated `.claude/settings.json`, `.codex/` and generated
`tests/E2ETests/**/*.feature.cs` changes throughout delivery. The repository workflow runtime is absent, so
this maintained plan is being executed through its standalone ledger path.

## Next Steps

Scope: complete the current P1 candidate and its terminal delivery; the full multi-phase plan remains incomplete.
Current slice: reconcile P1 4.8 with current `origin/main`, execute 4.9-4.10 qualification and canonical review,
then deliver PR #18 through exact-head validation and merge.
Remaining scope: P2-P5 retain their roadmap ownership after this PR.
Done when: the reconciled candidate passes the required local and remote gates and PR #18 is merged.

Checkpoint the backend candidate while excluding unrelated files, merge current `origin/main`, resolve the
legacy SQL Server composition onto the default branch's PostgreSQL graph, then rerun architecture, unit,
integration and broader qualification gates. Address canonical review findings before one stable push; verify
the exact pushed SHA remotely before merge.

## Completed work

- Slice 1, commit `5ab4356b`: membership snapshots, audiences, module-local exact grants, principal issuance,
  Concert summary sharing/member assignment and deletion of generic Application/Booking sharing.
- Slice 2, commit `b2001b2f`: deleted ambient host privilege; explicit privileged processing, published
  Concert projection and outcome receipt correctness.
- Slice 3, commit `f6ecc9bf`: regenerated access migrations, authority view, grant keys/indexes and seeding
  stances.
- Slice 4: independent ordinary connections plus one enlisted root command transaction, quiescent
  event/outbox flushing, authority validation and settlement/invoice atomicity.
- Slice 4.3: Application, Booking and Concert mutations use exact command policy, resource/grant locks,
  durable share replay and provider-real race coverage; `ApplicationSide` is deleted.
- Slice 4.1, this commit: typed permission identity end to end, strict ASP.NET name parsing, typed catalogs and
  client serialization at the wire boundary, with value/policy/catalog tests.
- Slice 4.7, this commit: explicit Conversation identity/audience, idempotent create/send, exact grants,
  sequenced messages, monotonic read positions, Tenant displays, invalidation-only delivery and complete
  backend/frontend/schema/guidance cutover.
- Slice 4.8 client cutover, commit `6d466a87`: authenticated Business web/mobile journeys, tenant session
  boundaries and neutral tenant naming across shared clients.

## Verification

- Pre-reconciliation `dotnet build Concertable.B2B.slnx --no-restore -m:4`: passed, 0 errors; 3 pre-existing
  E2E nullable warnings.
- Every backend unit-test project passed sequentially: 497 tests total.
- Architecture found one missing direct `Reunion.Errors` ownership reference in Tenant Infrastructure; the
  reference is restored and awaits the post-reconciliation rerun.
- Tenant integration initially failed to copy `Testcontainers.MsSql` from the B2B-owned fixture support
  library. `CopyLocalLockFileAssemblies` restores its runtime dependency closure, proven in the consumer output.
- Tenant integration then reached host startup and exposed the real current-graph gate: package-owned Inbox
  migrations are PostgreSQL while this stale branch still configures SQL Server. Current `origin/main` owns
  the durable PostgreSQL replacement and must be merged before rerunning integration.

Still required before a P1 completion claim: current-main reconciliation, architecture and all affected unit /
integration gates, provider race/revocation ordering, workers, contract/invoice reads, external-summary denial,
real browser/native evidence and canonical review. The complete Concert and Lifecycle tiers retain their
recorded 4.10 rerun obligations. The Startup resource-graph case must be rechecked where Stripe CLI is available.

## Reviews

No review yet for the P1 candidate; canonical review follows a green reconciled graph.

## Decisions, discoveries, blockers, and deviations

- Tenant remains the business/legal/membership/settlement identity; tokens remain identity-only.
- Eligibility, membership permission and resource audience remain separate facts.
- No ambient host bypass, query-filter bypass, generic private-details endpoint or unchecked load/save.
- P1 external disclosure is Concert Summary only; member assignments stay inside a principal tenant.
- Protected commands use one local transaction; ordinary and parallel reads use independent connections.
- External payment/blob calls remain outside the database transaction and reuse durable operation identity.
- The product is pre-launch: no compatibility layer, backfill or retained old vocabulary.
- Migrations stay owned by the filtered context; privileged contexts perform explicit system work.
- The architecture gate initially found a stale direct `Reunion.Validation` package reference in Concert unit
  tests; no source consumed it, so it was deleted and the gate reran green.
- Platform Testing `0.2.0-alpha.0.14` publishes only its PostgreSQL fixture; B2B's temporary SQL Server fixture
  must own and copy its full runtime dependency closure rather than relying on a nonexistent shared SQL type.
- The default branch has already replaced B2B SQL Server composition with PostgreSQL. Suppressing the Inbox
  pending-model warning or creating SQL Server migrations would preserve the obsolete graph; merging current
  `origin/main` is the durable repair.

## External/deferred owners

P2-P5, dependency publication, configurable-workflow documentation, configurable RBAC, tenant retirement,
sales evidence, payment quote disclosure and transport qualification retain their existing plan owners and
gates. No sibling checkout is modified by this P1 run.
