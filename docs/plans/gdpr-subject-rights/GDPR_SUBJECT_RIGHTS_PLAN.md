# B2B subject export delivery

## Ownership and authority

This plan owns the B2B-local work salvaged from PR #14. The PR-disposition owner is authorized by the user's 2 October request to act. The audit goal at `plans/pr-audit/PR_AUDIT.md` owns the wider PR cleanup. Preserve the original backlog at commit f8e709536b3d62aa9cc362e73aa2d2076fc7e0d8; its cross-service design and historical review remain reachable there.

## Candidate scope

- An admin-only JSON export reads the subject's User profile, Tenant memberships, authored messages and bounded Booking/Concert metadata through owning module facades.
- Contract export contains deal type and creation time; both counterparty names are omitted.
- Privacy coordinates a read-only export and owns no database schema. No erasure endpoint, timer, journal, migration or mutation hook lands with this export.

Erasure is retired from the delivered candidate. Fresh security review found that deleting the sole owner can strand a shared tenant and that membership changes can widen fan-out beyond checked financial obligations. Safe erasure requires a coordinated membership/financial fence and ownership handover policy. Original and repaired erasure code, tests and migrations remain reachable in existing PR history, including f8e709536b3d62aa9cc362e73aa2d2076fc7e0d8 and ef1868da9.

This is a B2B-local capability. Auth credentials/sessions, Payment and Customer data, retention scheduling and policy ratification remain outstanding. This candidate does not close the GDPR launch requirement or establish full compliance.

## Excluded backlog work

ActionLink refactors, Vite helper moves, architecture-test naming changes, commercial documents and unrelated test classification changes are removed. Original source history is preserved. The active PartyFoundation #38–#42 stack and #18 recovery branch retain their separate owner.

## Qualification checkpoint

- Historical erasure repair qualification passed 39 unit and seven Privacy provider cases, plus Booking handoff, Concert obligations and production migration provider projects. These qualify the preserved checkpoint, not the final export-only candidate.
- Final export authorization, authored-message isolation, unknown-subject behavior and conservative financial metadata require fresh provider qualification.
- Current immutable source/security review, architecture/startup checks and full CI remain required before landing.

## Next steps

1. Freeze and review the export-only main-relative candidate, especially administrator authorization and subject isolation.
2. Repair retained findings and run focused regressions.
3. Publish one stable candidate, qualify exact-head CI and land through the normal merge queue.
4. Record the actual outcome in this plan and the audit goal.
