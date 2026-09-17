# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Worktree: `C:/Users/TommySeery/source/repos/Concertable/b2b/.worktrees/Refactor-PartyFoundationLegacyBindings`
- Branch: `Refactor/PartyFoundationLegacyBindings`
- Reviewed base: `309e40d4b4b704fe94246332130566b89f464de4`
- Reviewed implementation head: `189f745d8233b511273624d768bcc39b5508723b`
- PR: [#18](https://github.com/Concertable/b2b/pull/18).
- Last reconciled: 2026-09-17, user-requested Claude Opus implementation handoff.
- Current authorization: implement, verify and locally commit the P1 feedback and agreed replacement
  mechanisms, including naming corrections, in an independent Claude Opus session.
- Delivery gate: the user's original no-push/no-merge restriction remains; the new request authorizes
  P1 implementation, not PR #18 delivery or P2–P5 execution.
- Future dependency/package gates: recheck overlapping OperationClaim/vocabulary owners; P2
  Concert/Seed/Hosting publication plus Customer/Search consumption; configuration persistence's
  provider choice remains with its owner.

## Current state

Keep this branch; substantially rework P1. The reviewed 16 commits change 411 files. The plan's
section 2 records 28 confirmed source findings and area-by-area keep/replace/remove decisions.
Section 4 supplies the replacement mechanisms, code, naming inventory and required verification.
P1 is not complete. P2–P5 remain unimplemented targets.

Already present in source, without implying qualification:

- Neutral Authorization and Tenant membership/profile separation; Tenant contact/version fields.
- Six module-owned grant families replacing pair-scoped visibility and the deleted payee resolver.
- Thread-based conversations, client business-profile changes and server-returned permissions.
- Request-wide shared connection, Concert/Application/Booking sharing and fixture seeding changes.

Those are implementation facts, not accepted designs. The replacement preserves the useful ownership
boundaries, replaces ambient privilege and transaction wiring, repairs scope/command authorization,
renames/reworks Conversation, and finishes the actual client journeys. Application/Booking sharing
and RestrictedParticipant are removed from P1. Configurable multi-role RBAC has a separately scoped
follow-on contract in plan section 4.1.

The old ledger's claims that no PR exists, clients/shares remain uncommitted, Contract grants are
issued, the authority view is read-only, or all authority/access slices are delivered are superseded.
The 15 September restart is history; do not recover discarded LegacyFinancialParties commits.

## Next Steps

Transfer: Apply the P1 feedback and accepted replacement mechanisms in the existing branch.
Transfer to: Independent Claude Code session using Opus in the declared worktree.
Resume stage: P1 implementation repairs and qualification, starting with plan section 4.1.
Resume when: Claude has loaded this ledger and the full plan in the declared worktree.

Scope: current slice only; full plan remains incomplete.
Current slice: P1 review repairs, agreed naming corrections and qualification.
Remaining scope: PR #18 delivery, then P2–P5 and their delivery gates.
Done when: F01–F28 are repaired, verified against section 4.10 and reviewed; changes are locally committed
and this ledger records the actual results and remaining delivery gate.

Apply the P1 replacement specification and close the review findings. The user explicitly requested
Claude Opus to address this feedback and apply these changes; the earlier review-only limit is
superseded for P1 implementation. Preserve the agreed design and use section 4 as the implementation
contract, including its naming inventory; recheck source where required and resolve concrete conflicts
without restarting the plan. The specification and naming correction are committed as 2ae4faa6 and aa4a3f2e.

Continue from the existing branch:

1. Recheck actual branch/worktree state and overlapping owners. Preserve unrelated dirty
   CODE_PATTERNS.md and .codex content. Read repository conventions before editing and retain the
   useful P1 foundation identified in section 2.
2. Repair membership incarnation, audience-aware permissions, same-row exact-scope read predicates,
   keyless membership authority and missing Contract principal issuance (4.1–4.2).
3. Add actor policies, the command-scoped local transaction and membership/resource fence; remove
   Application/Booking shares; implement only Concert Summary sharing plus own-member operational
   assignment, with replay, expiry and version behavior (4.3–4.5).
4. Replace ambient execution scope with explicit privileged repositories and fenced services; wire
   CompletionRunner, SettlementService, InvoiceIssuer, payment outcome processors and the fixture. Keep
   outbox insertion on the enlisted business context; do not treat the sibling AddOutbox overload
   as the atomicity fix (4.6).
5. Cut Thread over to ConversationId-addressed flows, immutable initial audience, message sequence,
   monotonic member read position, tenant display and safe delivery (4.7).
6. Finish neutral onboarding, activity/contact administration, invitation role policy and real
   Business web/mobile journeys; fix financial/admin DTOs and tenant-switch isolation (4.8–4.9).
7. Regenerate InitialCreate/fixtures and qualify the actual replacement with the full 4.10 matrix.
   Record source head, commands/results and browser/native evidence; review the committed candidate.
   Do not call P1 complete merely because existing unit tests/builds pass.

P1 completion: every F01–F28 finding closes through its named mechanism and test;
a zero-profile third business reads only the shared summary; all principal/assigned-member workflows
retain their correct permissions; workers and outbox are reliable without bypassing interactive
tests. Stop at the retained delivery gate after P1 qualification; P2's accepted participants/Show
and published consumer closure remain outside this handoff.

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
- New naming and exact before/after mechanisms are in section 4; do not re-invent them from this ledger.
- Membership snapshot queries belong to the existing MembershipRepository through Authorization's narrow
  IMembershipReadRepository contract. The proposed MembershipLookup abstraction is removed; membership
  resolution and administration keep their respective owners.
- No production compatibility layer, old schema reader, adapter or synthetic-data backfill is needed.

## Completed work

- This checkpoint: reviewed the frozen P1 candidate, recorded F01–F28, and specified replacement mechanisms with code. No runtime phase is declared delivered.

## Verification

This checkpoint is source analysis and specification. No runtime behavior was changed or runtime
tests executed as part of it. Historical counts reported by earlier agents concern older candidates
and do not qualify this re-specification or resolve the reported Concert integration failures.

Planning validation passed on 2026-09-17: the installed plan-graph validator reported zero errors and
warnings; the Workflow v2 repository provider validated this plan, ledger, worktree, branch and limited
next action. Document checks passed for section/finding coverage, closed code fences, local links and
ledger size. The naming audit uses the installed C# naming, persistence and multitenancy standards:
settlement remains a service; entity persistence uses the existing repository stances and aliases.
Git diff --check passed. No canonical isolated review is claimed; source evidence and the bounded
authority sanity check are distinct from that workflow.

Implementation acceptance is the plan's section 4.10. Real-provider authority/transaction races,
workers, contract/invoice reads, external-summary denial boundaries and real browser/native journeys
remain mandatory. PR CI does not automatically supply the separate Api/Ui E2E evidence.

## Reviews

Current review: source review and P1 re-specification, with a bounded authority sanity check. F01–F28 remain implementation findings owned by the plan's section 4; no runtime approval is granted. The [existing review artifact](../../reviews/Refactor-PartyFoundationLegacyBindings.md) records earlier candidates and is not a completed canonical review of this checkpoint.

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
No sibling checkout was edited by this planning task.

Tenant retirement/account teardown, authoritative sales evidence, payment quote disclosure,
transport qualification, richer admin roles and configurable RBAC retain their explicit owners and
completion gates. None is silently declared fixed by this resource-access foundation.
