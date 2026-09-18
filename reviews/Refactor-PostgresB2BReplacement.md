# Code review  Refactor/PostgresB2BReplacement

> **This file is a work order, not a discussion.** Fix open `[ ]` findings directly and tick each
> `[x]` when landed.

**Review status:** `complete`
**Judgment:** `approved`

## Review pass  2026-09-18  staged full

**Candidate branch:** `Refactor/PostgresB2BReplacement`
**Candidate base:** `de2477327f6578f11706ffad30a1ff7face0466c`
**Candidate head:** `15bcdde6`
**Candidate scope:** `all`
**Work-order path:** `reviews/Refactor-PostgresB2BReplacement.md`
**Work-order mode:** `new`
**Pass judgment:** `approved`

The canonical workflow helper is absent from this carved repository, so the review used the manual
staged fallback over one immutable base-to-head range. The areas were provider/foundation and the
migration job; module mappings and generated migrations; tests, local composition, CI and release
configuration. A separate security pass covered connection-string handling, dynamic SQL identifier
quoting, workflow inputs, artifact selection and secret scanning.

### Findings

No findings.

Considered and not raised:

- PostgreSQL prepared transactions are required by the existing cross-context `TransactionScope`
  boundaries. Local development and integration tests set `max_prepared_transactions=100`; managed
  PostgreSQL configuration remains in the already-created infrastructure companion branch.
- SQL Server remains in the local AppHost only for Auth and Payment. B2B now receives the PostGIS
  database, and its web and worker resources wait for the B2B migration job to complete.
- The backend category filter skips eight untagged test projects. Those projects were run explicitly
  for this candidate, and the workflow defect is recorded in `TECH_DEBT.md` with an objective
  resolution condition.
- `PostgresIdentitySequences` builds its catalog predicate from parameters and quotes discovered
  identifiers before constructing `setval` statements. No user-controlled identifier enters the SQL.
- All eleven pre-launch `InitialCreate` migrations were replaced through EF tooling. Provider-residue,
  PostGIS, xmin, owned-schema and per-context history-table scans matched the intended PostgreSQL model.

Verification: Release solution build; exact backend CI filter; all eight otherwise-unselected test
projects; Conversations integration regression after the UTC-kind repair (16/16); migration validation;
migration job twice against a clean PostgreSQL database with thirteen history tables; twelve packed
NuGet candidates and clean consumer restore/build; four OCI archives; promotion-config validation;
Syft/Trivy/hash/secret integrity checks; startup graph 16/16; generated Reqnroll output clean; and
`git diff --check`.
