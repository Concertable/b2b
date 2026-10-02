# B2B subject-rights delivery

## Ownership and authority

This plan owns the B2B-local work salvaged from PR #14. The PR-disposition owner is authorized by the user's 2 October request to act. The audit goal at `plans/pr-audit/PR_AUDIT.md` owns the wider PR cleanup. Preserve the original backlog at commit f8e709536b3d62aa9cc362e73aa2d2076fc7e0d8; its cross-service design and historical review remain reachable there.

## Candidate scope

- An admin-only JSON export reads the subject's User profile, Tenant memberships, authored messages and bounded Booking/Concert metadata through owning module facades.
- Contract export contains deal type and creation time; both counterparty names are omitted.
- B2B-local erasure records a request, defers while Application/Booking/Concert report live obligations, severs memberships and authored-message identity, removes pending invitations, scrubs participant profiles for empty tenants and anonymises the User profile.
- The hourly worker rechecks resumable requests in fresh scopes.
- The Booking hand-off acknowledgement protects the gap before Concert creation is observed.
- Privacy has its own PostgreSQL schema. Booking and Conversations keep their current prelaunch PostgreSQL InitialCreate authority.

This is a B2B-local capability. Auth credentials/sessions, Payment and Customer data, retention scheduling and policy ratification remain outstanding. This candidate does not close the GDPR launch requirement or establish full compliance.

## Excluded backlog work

ActionLink refactors, Vite helper moves, architecture-test naming changes, commercial documents and unrelated test classification changes are removed. Original source history is preserved. The active PartyFoundation #38–#42 stack and #18 recovery branch retain their separate owner.

## Qualification checkpoint

- Privacy unit tests: 36 passed.
- Privacy PostgreSQL integration tests: 5 passed, including admin route authorization, deferral, erasure and export.
- Conversations, Booking and Privacy migrations regenerated with the repository owner helper; all resolved paths verified inside the delivery checkout.
- Current source review, affected integration/architecture checks and full CI remain required before landing.

## Next steps

1. Freeze and review the narrowed main-relative candidate, especially authorization, crash recovery and live-obligation races.
2. Repair retained findings and run focused regressions.
3. Publish one stable candidate, qualify exact-head CI and land through the normal merge queue.
4. Record the actual outcome in this plan and the audit goal.
