# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Worktree: \`C:\Users\TommySeery\source\repos\Concertable\b2b\.worktrees\Refactor-PartyFoundationLegacyBindings\`
- Branch: \`Refactor/PartyFoundationLegacyBindings\`
- PR: [#18](https://github.com/Concertable/b2b/pull/18)
- Reviewed base: \`309e40d4b4b704fe94246332130566b89f464de4\`
- Current checkpoint: b2b-web's reset pauses the whole web host, but review N16 proved the deadlocking
  settlement runs in B2B Workers; quiescing Workers across the reset is routed to Astra for design
- Delivery gate: authorized through canonical review, commit, push, exact-head remote validation and merge
- Last reconciled: 25 September 2026 against \`origin/main\` \`a1baf9a7\`
- Ownership: transferred 25 September 2026 to a fresh Claude session in this worktree; no other writer is active

## Current state

P1 slices 1-4, 4.3, 4.1, 4.7 and 4.8 are implemented. The branch has absorbed the current default
branch's PostgreSQL composition. Canonical review covered all 757 manifest paths; accepted code findings
N1-N15 are repaired, locally validated, committed and approved by both native/general and
security/durability lenses. Of the later findings, N16 (Workers not paused by the reset) is open.

The default branch's PR #32, #27 and #35 are merged into this branch at \`1d27cb87\`. The E2E reset defect
is half repaired: b2b-web now pauses every source of its own work, but the settlement that deadlocks the
truncate runs in the separate B2B Workers process, which nothing pauses yet.

The provider reconciliation uses the platform PostgreSQL fixture plus B2B-owned database lifecycle,
Npgsql command transactions and an NTS-configured \`NpgsqlDataSource\`. Command contexts are reset to
independent owned connections after commit or rollback. PostgreSQL row locks and the atomic conversation
read-position upsert replace SQL Server lock hints. All eleven current InitialCreate snapshots are aligned
to PostgreSQL; Tenant's migration retains the hand-authored \`tenant.MembershipAuthority\` view.

Preserve the unrelated \`.claude/settings.json\`, \`.codex/\` and generated
\`tests/E2ETests/**/*.feature.cs\` changes throughout delivery. The first two remain in stash
\`4fecf8ee3a850423cec8959d279b6ff75bc24097\`; the generated-file backup is
\`3c04f1a67ed73382eb729724120ce9ed1e0b2996\`, and the current generated feature files remain unstaged.

## Next Steps

Scope: current slice only; full plan remains incomplete.
Current slice: close the E2E reset defect and deliver the reconciled head of PR #18.
Remaining scope: P2-P5 remain future roadmap phases after this P1 delivery.
Done when: one reviewed head passes exact-head remote CI and E2E gates and PR #18 is merged.

1. **Done: the published host pause is consumed.** Platform is `0.2.0-alpha.0.20` with
   `Concertable.Messaging.AspNetCore` added. `AddB2BE2EAdmin` registers `AddHostPauser()` and `AddGate`
   exempting `/_e2e`, `/health` and `/alive`; `MapB2BE2EAdmin` became `UseB2BE2EAdmin`, which inserts
   `UseGate()` ahead of the whole host pipeline. The reset pauses through `HostPauser` inline in its request
   and resumes in `finally`. `NoOpBusQuiescence` is deleted, and a new `E2EAdminApiTests` case drives the real
   reset against PostgreSQL. The bump was **not** source-compatible, contrary to the earlier note: platform #23
   deleted `UseSeedingSupport` (its only body registered the SQL Server identity-insert interceptor) and moved
   `SeedingScope` out of `Concertable.Seed.Shared.Identity`, so all nine module registrations and
   `DevDbInitializer` changed with it. Names are fixed: Tommy rejected "quiescence/ingress", `-able`
   implementation names and "Composite".
2. **Done: incremental review `e0cfe596..0822900d`** — judgment `changes-requested`. N17 (exempt `/hub`)
   and N18 (assert the reset releases the gate) are repaired. **N16 is open and blocks delivery.**
3. **Next: quiesce B2B Workers across the reset (N16) — design routed to Astra.** The 5efaa089 diagnostics
   show `ConcertFinishedFunction`, fired by a test through the Functions admin API and returned on 202,
   settling 22 seeded concerts from 11:31:11.027 to past 11:31:14 while b2b-web reset at 11:31:11.874 and
   11:31:13.021; `InvoiceIssuer`'s `InvoiceSequences` read is that process, not b2b-web. `HostPauser` is
   per-process. The design must decide how a reset in b2b-web (or the E2E harness driving it) stops and
   drains an Azure Functions isolated host that shares the database: e.g. a Workers-side `IPausable` gate over
   function invocations reached through an E2E-only endpoint, or the harness awaiting the triggered
   invocation's completion, or scoping the triggered run. The design comes back as code snippets under this
   step; Claude then implements it, runs the E2EAdmin/Workers tiers, and appends the incremental review.
4. Push, require ordinary CI plus separately dispatched `.github/workflows/e2e.yml` at that exact SHA, then
   merge PR #18 and restore the preserved unrelated files without committing them.

Local builds, unit, architecture, startup and single-project integration tiers run on this workstation even
with under 1 GB free; the full integration suite and the Aspire E2E stack are validated remotely.

## Completed work

- Slice 1, commit \`5ab4356b\`: membership snapshots, audiences, module-local exact grants, principal issuance,
  Concert summary sharing/member assignment and deletion of generic Application/Booking sharing.
- Slice 2, commit \`b2001b2f\`: deleted ambient host privilege; explicit privileged processing, published
  Concert projection and outcome receipt correctness.
- Slice 3, commit \`f6ecc9bf\`: regenerated access migrations, authority view, grant keys/indexes and seeding.
- Slice 4: independent ordinary connections plus one enlisted root command transaction, quiescent
  event/outbox flushing, authority validation and settlement/invoice atomicity.
- Slice 4.3: Application, Booking and Concert mutations use exact command policy, resource/grant locks,
  durable share replay and provider-real race coverage; \`ApplicationSide\` is deleted.
- Slice 4.1: typed permission identity end to end, strict ASP.NET name parsing, typed catalogs and client
  serialization at the wire boundary, with value/policy/catalog tests.
- Slice 4.7: explicit Conversation identity/audience, idempotent create/send, exact grants, sequenced messages,
  monotonic read positions, Tenant displays, invalidation-only delivery and complete contract cutover.
- Slice 4.8, commit \`6d466a87\`: authenticated Business web/mobile journeys, tenant-session boundaries and
  neutral tenant naming across shared clients.
- Backend completion checkpoint \`d073fb75\`: neutral tenant lifecycle, resource access, conversation and
  client journeys before default-branch reconciliation.
- Current-main reconciliation: PostgreSQL hosting/migrations/fixtures, provider-correct locks, geometry mapping,
  command-transaction connection ownership and current composition merged from \`7fd22b46\`.
- Canonical remediation N1-N14: command cancellation/locking, provider-real races, invitation seeding,
  PostgreSQL E2E lookup, tenant-session clearing, exact frontend roles and mobile permission/auth navigation.
- Delivery repairs on this branch: \`a387e19a\` declared the Reunion reference the Application tests use;
  \`0e9f9d05\` pinned auth to the image that knows the Business client, which every API E2E test had been
  failing its readiness poll without; \`f580b53f\` named this branch's new contracts in the release set and
  declared \`TenantDisplayChanged\`/\`ConversationChanged\` in the bus topology, which had left their
  subscriptions unprovisioned and fan-out dead; \`ceb13880\` kept the messaging migration history across an
  E2E reset (42P07); \`85c7c5a9\` took the release candidate set from the promotion manifest alone, ending
  the drift of four hand-maintained copies.
- E2E reset pause: `IBusQuiescence` (receiver only) was replaced by platform `HostPauser` plus the request
  gate, pausing all of b2b-web's own work; B2B Workers remains unpaused (N16).

## Verification

- Platform `0.2.0-alpha.0.20` head, 25 September 2026: `dotnet build Concertable.B2B.slnx` passed with 0
  errors; the tagged unit tier, E2EAdmin integration 9/9 (including the real PostgreSQL reset),
  architecture 24/24 and startup 16/16 passed. Free memory was under 1 GB, but these tiers completed.
- Ordinary remote CI passed at `15dce560`, covering build, unit and integration on the reconciled graph.
- Before the reconciliation merge, every local tier was green: unit 498/498, architecture 24/24, startup 16/16,
  integration 435/435 across 14 projects, provider race regressions, migration drift across all 11 contexts,
  and the full web and mobile gates. Those runs are evidence about the P1 code, not about the merged graph.
- Windows needs a process-local shortened PATH for nested npm wrapper scripts in the full frontend gates.
- The backend CI category filter skips seven untagged projects; run directly they pass. `TECH_DEBT.md` owns
  the correction.
- Exact-head remote CI and the separately dispatched API/UI E2E workflow remain delivery gates.
- `GITHUB_PACKAGES_TOKEN` in this environment is an expired `ghp_` PAT (every `Concertable.*` restore returns
  401); export `gh auth token` into it for restores until whoever owns the PAT replaces it.

## Reviews

Canonical review covered all 757 manifest paths and accepted N1-N15. N1-N14 are repaired and approved by both
native/general and security/durability lenses through commit \`ed76eda6\`. N15 is this planning-graph
reconciliation; after its incremental approval, current-graph qualification and the final canonical pass remain.
Existing \`reviews/Refactor-PostgresB2BReplacement.md\` belongs to the merged default-branch provider work and
does not substitute for the P1 review.

## Decisions, discoveries, blockers, and deviations

- Tenant remains the business/legal/membership/settlement identity; tokens remain identity-only.
- Eligibility, membership permission and resource audience remain separate facts.
- No ambient host bypass, query-filter bypass, generic private-details endpoint or unchecked load/save.
- P1 external disclosure is Concert Summary only; member assignments stay inside a principal tenant.
- Protected commands use one local transaction; ordinary and parallel reads use independent connections.
- External payment/blob calls remain outside the database transaction and reuse durable operation identity.
- The product is pre-launch: a superseded shape is replaced outright, with no parallel path or data retrofit.
- Migrations stay owned by the filtered context; privileged contexts perform explicit system work.
- Npgsql geometry requires an NTS-configured data source for raw command transactions, not a replacement
  unconfigured connection.
- Enlisted contexts must be detached from the root transaction connection after completion so later scoped
  reads do not reuse a disposed connection.
- PostgreSQL \`FOR UPDATE\` protects mutable command facts, \`FOR SHARE\` protects membership-authority fences,
  and conversation read positions use \`ON CONFLICT ... GREATEST\`.
- Opportunity creation is restricted to VenueOperator activity; integration handlers and race verification use
  privileged contexts when no interactive tenant exists.
- **The reset 500 is a lock-order inversion between the truncate and a live settlement.** At \`5efaa089\`,
  \`ConcertFinishedTests\` failed its \`InitializeAsync\` reset with \`40P01\`: Respawn's \`TRUNCATE ... CASCADE\`
  waited for \`AccessExclusiveLock\` while \`InvoiceIssuer\`'s \`concert."InvoiceSequences"\` read waited for
  \`AccessShareLock\`. A writer surviving the pause also leaves dirty state after the truncate. The
  \`e2e-diagnostics.log\` artifact of run 35721355090 carries the full report and the Workers timeline.
- **The database-level fence is disproven, with evidence.** \`127600fa\` had the reset terminate every
  other backend on the database before truncating, so that its contract -- nothing else holds a
  transaction across the truncate -- would be literally true. Two dispatched E2E runs rejected it, both
  strictly worse than the 3 failures it set out to fix, at 10 of 10 failing at fixture startup:
  \`127600fa\` alone died reseeding through a connection pooled before the cull (\`57P01\` in
  \`NpgsqlHistoryRepository.GetAppliedMigrationsAsync\`), and \`f400b498\`, which discarded both pools in
  the same breath, left b2b-web silent immediately after \`Service Bus consumption paused across 18
  processors\` -- no resume, no exception, no further output. Reverted at \`8a3143a3\`.
- A test endpoint cannot cull the runtime it runs in: "every other backend" includes b2b-web's own
  hosted services and bus receiver, and clearing pools does nothing for work already holding a connection.
- **The earlier "in-flight HTTP request was the deadlocking writer" diagnosis was wrong.** The other party
  was B2B Workers' `ConcertFinishedFunction` (review N16). The web-host pause is still needed, since it
  stops b2b-web's own receiver, outbox and requests, but it is not sufficient.
- **The ingress pause covers b2b-web, delivered as platform `0.2.0-alpha.0.20`.** The reset keeps
  `PauseAsync` inside its `try` so `finally` always resumes every pausable. That matters because
  `HostPauser`'s rollback skips the pausable whose own `PauseAsync` threw, and `GateMiddleware` sets its
  paused flag before awaiting the drain, so a cancelled reset would otherwise leave the gate holding every
  request. The platform defect is recorded in platform-dotnet `src/Concertable.Messaging/TECH_DEBT.md` through
  [platform-dotnet #30](https://github.com/Concertable/platform-dotnet/pull/30), open for Tommy's review.
- The two FlatFee checkout 409s are not attributable to this branch. A control run of the default branch
  plus only the Outbox fix scored 8 of 10 with exactly those two failing, against 6-7 of 10 here. The cause
  is the checkout operation identity being composed from a database id that Respawn reseeds, so a reused id
  collides with a retained Payment operation. That is the unstable-checkout-ID debt P2 already owns at
  \`PARTY_FOUNDATION_PLAN.md\` section P2.
- The default branch's `ITenantScope` is rejected rather than merged. PR #32 introduced it as an AsyncLocal
  a request-less writer sets to name the tenant it writes as, which is the ambient-authority shape section 4.6
  deletes; this branch had already answered the same question with composed privileged stances, and carrying
  both would leave two mechanisms for one job. `ITenantScope`, `TenantScope`, `TenantScopedSeeding` and the
  callers that arrived with them are deleted. Verified as lossless: main's every conflicting change was that
  mechanism, its `SetConcertPeriodAsync` and `AsSettlementPayeeAsync` fixture helpers only wrap what this
  branch does through the privileged context, and its `ConcertCompletionCandidate` duplicates what
  `IConcertReadRepository` already reads off the unfiltered stance.
- Auth moves to `0.2.0-alpha.0.305`. The 0.304 pin existed only because the E2E harness still handed auth a
  SQL Server database; PR #35 moved it to PostgreSQL and deleted the fixture's separate auth override, so the
  reason for the pin is gone.
- N16 is the one open accepted finding. Current-graph qualification and final review remain delivery gates.

## External/deferred owners

P2-P5, dependency publication, configurable-workflow documentation, configurable RBAC, tenant retirement,
sales evidence, payment quote disclosure and transport qualification retain their existing plan owners and
gates. No sibling checkout is modified by this P1 run.
