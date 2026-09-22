# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Worktree: \`C:\Users\TommySeery\source\repos\Concertable\b2b\.worktrees\Refactor-PartyFoundationLegacyBindings\`
- Branch: \`Refactor/PartyFoundationLegacyBindings\`
- PR: [#18](https://github.com/Concertable/b2b/pull/18)
- Reviewed base: \`309e40d4b4b704fe94246332130566b89f464de4\`
- Current checkpoint: P1 implementation, remediation and N15 reconciliation complete; delivery gates active
- Delivery gate: authorized through canonical review, commit, push, exact-head remote validation and merge
- Last reconciled: 22 September 2026 against merged \`origin/main\` \`7fd22b46\`

## Current state

P1 slices 1-4, 4.3, 4.1, 4.7 and 4.8 are implemented. The branch has absorbed the current default
branch's PostgreSQL composition. Canonical review covered all 757 manifest paths; accepted code findings
N1-N15 are repaired, locally validated, committed and approved by both native/general and
security/durability lenses. No accepted finding remains open.

Ordinary CI is green. The one E2E defect attributable to this branch was the reset endpoint racing
handlers that still held this branch's row locks, and it is repaired through a new platform capability
rather than a retry or a widened timeout.

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
Current slice: land the E2E reset quiescence fix, complete the final incremental review and deliver PR #18.
Remaining scope: P2-P5 remain future roadmap phases after this P1 delivery.
Done when: one reviewed head passes local and exact-head remote CI/E2E gates and PR #18 is merged.

1. Bump the platform pin to the published quiescence release, rebuild and re-run the backend gates.
2. Re-run the API E2E suite and confirm \`ConcertFinishedTests\` no longer 500s on the reset endpoint.
3. Append the final incremental review pass for base \`ed76eda6\` through the delivered head.
4. Push, require ordinary CI plus separately dispatched \`.github/workflows/e2e.yml\` at that exact SHA,
   merge PR #18 and then PR #27, and restore the preserved unrelated files without committing them.

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
- E2E reset quiescence: the reset endpoint now pauses inbound bus consumption, waits for every running
  handler to finish, resets, and resumes. The capability is \`IBusQuiescence\` in
  \`Concertable.Messaging.Contracts\`, implemented by the Azure Service Bus receiver over
  \`StopProcessingAsync\`/\`StartProcessingAsync\`.

## Verification

- Restore: \`dotnet restore Concertable.B2B.slnx --force-evaluate\` passed.
- Current-graph build: \`dotnet build Concertable.B2B.slnx --no-restore -m:4\` passed with 0 warnings and
  0 errors in 5m10s.
- Unit: all 14 backend UnitTests projects passed, 498/498.
- Architecture: 24/24 passed.
- Startup/resource composition: 16/16 passed, including the Stripe-aware resource graph.
- Integration: all 14 projects passed, 435/435 total:
  Tenant 90, Application 76, Booking 24, Concert 79, Conversations 17, Lifecycle 41, Admin 8,
  Artist 20, Dashboard 16, Deal 2, Opportunity 14, User 14, Venue 27 and E2EAdmin 7.
- Provider race regressions passed after their durable repairs: Application accept/reject, Booking/Concert
  command locking, monotonic Conversation reads, Opportunity activity authorization and Venue profile creation.
- Migration drift: \`scripts/validate-migrations.ps1 -Configuration Debug\` passed all 11 contexts.
- Prior full web gate: \`npm run build:web\` passed shared tests 37/37, web-shared tests 18/18 and all four
  production builds. Current-head affected gates pass shared 39/39, web-shared 18/18 and both package builds.
- Prior full mobile gate: \`npm run build:mobile\` passed TypeScript validation and Android export (3,864 modules).
  Current-head mobile navigation passes 4/4 and TypeScript validation.
- Windows requires a process-local shortened PATH for nested npm wrapper scripts; direct constituent package
  commands are green. The final full frontend gates must use the shortened PATH.
- Ordinary remote CI passed at \`85c7c5a9\`.
- The backend CI category filter still skips seven untagged projects. Run directly they pass 98 of 98
  across the six unit projects; the seventh, Conversations integration, is covered by the same entry.
  \`TECH_DEBT.md\` owns the stale affected-list correction.
- Exact-head remote CI and the separate API/UI E2E workflow remain delivery gates after the reviewed push.

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
- Platform Testing \`0.2.0-alpha.0.14\` exposes only its PostgreSQL fixture. The obsolete local SQL Server
  fixture and \`Testcontainers.MsSql\` dependency were removed during the durable provider reconciliation.
- Npgsql geometry requires an NTS-configured data source for raw command transactions, not a replacement
  unconfigured connection.
- Enlisted contexts must be detached from the root transaction connection after completion so later scoped
  reads do not reuse a disposed connection.
- PostgreSQL \`FOR UPDATE\` protects mutable command facts, \`FOR SHARE\` protects membership-authority fences,
  and conversation read positions use \`ON CONFLICT ... GREATEST\`.
- Opportunity creation is restricted to VenueOperator activity; integration handlers and race verification use
  privileged contexts when no interactive tenant exists.
- The reset endpoint deadlocked (40P01) because Respawn's DELETE raced row locks still held by handlers
  draining the previous test's bus messages, and stale deliveries landed after the reset and corrupted
  Payment state. Retrying the deadlock or widening a timeout was rejected: neither addresses the stale
  deliveries. Quiescence belongs to the receiving transport, so the capability was added to the platform
  messaging package and consumed here, accepting the publish-then-bump release that implies.
- The two FlatFee checkout 409s are not attributable to this branch. A control run of the default branch
  plus only the Outbox fix scored 8 of 10 with exactly those two failing, against 6-7 of 10 here. The cause
  is the checkout operation identity being composed from a database id that Respawn reseeds, so a reused id
  collides with a retained Payment operation. That is the unstable-checkout-ID debt P2 already owns at
  \`PARTY_FOUNDATION_PLAN.md\` section P2.
- No accepted code finding remains open. Current-graph qualification and final review remain delivery gates.

## External/deferred owners

P2-P5, dependency publication, configurable-workflow documentation, configurable RBAC, tenant retirement,
sales evidence, payment quote disclosure and transport qualification retain their existing plan owners and
gates. No sibling checkout is modified by this P1 run.
