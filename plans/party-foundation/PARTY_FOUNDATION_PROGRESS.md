# Party foundation progress

- Plan: \`plans/party-foundation/PARTY_FOUNDATION_PLAN.md\`
- Roadmap: \`plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md\`
- Roadmap item: \`party-foundation/core\`
- Worktree: \`C:\Users\TommySeery\source\repos\Concertable\b2b\.worktrees\Refactor-PartyFoundationLegacyBindings\`
- Branch: \`Refactor/PartyFoundationLegacyBindings\`
- PR: [#18](https://github.com/Concertable/b2b/pull/18)
- Reviewed base: \`309e40d4b4b704fe94246332130566b89f464de4\`
- Current checkpoint: P1 4.8 candidate reconciled with the PostgreSQL default branch and locally qualified
- Delivery gate: authorized through canonical review, commit, push, exact-head remote validation and merge
- Last reconciled: 20 September 2026 against HEAD \`d073fb75\` and merged \`origin/main\` \`7fd22b46\`

## Current state

P1 slices 1-4, 4.3, 4.1, 4.7 and 4.8 are implemented. The branch has absorbed the current default
branch's PostgreSQL composition and the merge-conflict repair has passed the complete local qualification
matrix. The merge remains open only until the reconciled changes and this ledger are staged and committed.

The provider reconciliation uses the platform PostgreSQL fixture plus B2B-owned database lifecycle,
Npgsql command transactions and an NTS-configured \`NpgsqlDataSource\`. Command contexts are reset to
independent owned connections after commit or rollback. PostgreSQL row locks and the atomic conversation
read-position upsert replace SQL Server lock hints. All eleven current InitialCreate snapshots are aligned
to PostgreSQL; Tenant's migration retains the hand-authored \`tenant.MembershipAuthority\` view.

Preserve the unrelated \`.claude/settings.json\`, \`.codex/\` and generated
\`tests/E2ETests/**/*.feature.cs\` changes throughout delivery. The first two are held in
\`stash@{0}\` while the merge is completed; generated feature files remain unstaged.

## Next Steps

Scope: complete the current P1 candidate and its terminal delivery; P2-P5 retain their roadmap ownership.
Done when: the reconciled candidate passes canonical review and exact-head remote gates and PR #18 is merged.

1. Stage the reconciled provider repairs, regenerated migrations and this ledger; exclude preserved user and
   generated feature files, then complete the local merge checkpoint.
2. Run canonical review against that immutable checkpoint and serially close any actionable findings with
   focused and affected-gate reruns.
3. Push one stable reviewed head, require ordinary CI and the separately dispatched \`.github/workflows/e2e.yml\`
   at that exact SHA, then merge PR #18.
4. Restore the preserved unrelated files without committing them and record terminal delivery.

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
- Web: \`npm run build:web\` passed shared tests 37/37, web-shared tests 18/18 and all four production builds.
- Mobile: \`npm run build:mobile\` passed TypeScript validation and Android export (3,864 modules).
- Windows required a process-local shortened PATH for nested npm scripts; source and lockfile were unchanged.
- Exact-head remote CI and the separate API/UI E2E workflow remain delivery gates after the reviewed push.

## Reviews

Canonical review has not started. It follows the local merge checkpoint so the reviewer receives one immutable
reconciled diff. Existing \`reviews/Refactor-PostgresB2BReplacement.md\` belongs to the merged default-branch
provider work and does not substitute for the P1 review.

## Decisions, discoveries, blockers, and deviations

- Tenant remains the business/legal/membership/settlement identity; tokens remain identity-only.
- Eligibility, membership permission and resource audience remain separate facts.
- No ambient host bypass, query-filter bypass, generic private-details endpoint or unchecked load/save.
- P1 external disclosure is Concert Summary only; member assignments stay inside a principal tenant.
- Protected commands use one local transaction; ordinary and parallel reads use independent connections.
- External payment/blob calls remain outside the database transaction and reuse durable operation identity.
- The product is pre-launch: no compatibility layer, backfill or retained old vocabulary.
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
- No current local red gate remains.

## External/deferred owners

P2-P5, dependency publication, configurable-workflow documentation, configurable RBAC, tenant retirement,
sales evidence, payment quote disclosure and transport qualification retain their existing plan owners and
gates. No sibling checkout is modified by this P1 run.
