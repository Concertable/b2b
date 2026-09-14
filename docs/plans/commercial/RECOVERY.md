# Recovered from the monorepo archive — supersession is an open question

Recovered 2026-09-14 from the archived monorepo branch `Docs/launch_booking-entry-direct-offers`
(16 commits, tip `4d82045bc`, last dated 2026-09-09). The branch was never merged, had no PR, and the
audit that preceded this port found its final 11 commits existed only in a local worktree. They are here
now; nothing in this directory had reached any polyrepo before today.

## What it is

`COMMERCIAL_EXECUTION_PLAN.md` is **4,418 lines** and carries an explicit approval:

> PostgreSQL persistence architecture approved on 9 September 2026 after independent source-based review:
> relational composition and typed references, bounded TPH families and immutable JSONB snapshots.
> AgreementSnapshot is the agreed immutable DTO name.

It describes itself as "the B2B-owned successor to the direct-offers draft, not a second plan running
alongside it", and covers B2B commercial configuration, templates, both booking entry paths, event/resource
context and the B2B PostgreSQL migration. Three SVG figures come with it.

## The open question

The handoff that triggered this port guessed that `DEAL_CONFIGURATION_PLAN.md` (in `Concertable/docs`)
"likely supersedes" this trail. **That is not established, and the evidence cuts both ways:**

- `DEAL_CONFIGURATION_PLAN.md` was last edited 2026-09-13, four days *after* this trail stopped — so it is
  the more recent document.
- But it is **509 lines to this plan's 4,418**, and it does not reference this plan, this directory, or
  `AgreementSnapshot` anywhere. A successor that folded in an approved architecture would normally cite it.

So this is preserved rather than dropped, and the supersession call is left open deliberately. Someone who
knows which design is actually being built should read both and either fold the surviving decisions into
`DEAL_CONFIGURATION_PLAN.md` and delete this directory, or promote this plan and retire the other.

Until that call is made, treat `DEAL_CONFIGURATION_PLAN.md` as the live plan — it is what the B2B
`AGENTS.md` product-direction section points at — and treat this as an approved-but-unintegrated design
record, not a second set of instructions.

## Vocabulary warning

This plan predates the deal-lifecycle carve landing and the deal-vocabulary collapse. Read it for its
decisions and their reasoning, not for its symbol names.
