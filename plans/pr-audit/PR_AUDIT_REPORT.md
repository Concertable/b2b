# Open PR closure audit report

## Current delivery outcomes

The user authorized delivery after the original audit. #28 and #29 are merged with passing causal main CI; together they contain all ten Renovate update sets. Eight superseded original PRs are closed. #14 is being qualified as a focused admin subject export with counterparty names and identifier-bearing invoice numbers omitted; erasure is retired from the delivered tree and preserved in branch history. The recovery/foundation PRs remain with their separate owners. The audit recommendations below are historical evidence.

Observed inventory: **2 October 2026, 13:38:58 UTC**. Comparisons completed against fixed SHAs below; this is a snapshot, not a live merge assessment.

**No unconditional closure candidates.** Of 17 open PRs: **16 KEEP, 1 HOLD, 0 CLOSE CANDIDATE**. #18 is the only prioritized conditional future closure candidate. #14 contains unique work; the five draft stack layers contain dependent changes, and all ten dependency updates remain unapplied. This establishes non-duplication, not product value or merge readiness.

## Scope and evidence anchors

- Repository: [Concertable/b2b](https://github.com/Concertable/b2b).
- Current remote main, read from GitHub and matched to locally available Git objects: `e01b979c120e45ffd2985a62f30db3c66b254191` (merge of #37, following #36).
- Cumulative published replacement stack: #42 at `262a086b47526b07ecac96158c712c8de0dd026c`.
- Audit checkout stays at `7876cb3462068268b4ae7ca7208df878639703b7` on `Refactor/PartyFoundationQuality`; checkout content was not used as current product evidence.
- One GitHub open-PR inventory refresh; read-only PR metadata/patch queries and local object comparisons. No fetch, build, restore, tests, code edits, commits, pushes, PR comments, closure, merge or branch/worktree deletion.
- Captured evidence: [inventory](inventory.json), [#14 metadata](pr14.json), [#18 metadata](pr18.json), [all changed-path comparisons](comparisons.json), [dependency comparisons](dependency-comparisons.json), [legacy-to-stack rename/path comparison](legacy-to-stack.paths), [legacy-to-stack statistics](legacy-to-stack.stat). Individual `pr<number>.diff` files retain #14 and the ten dependency patches. GitHub refused #18's full diff because it exceeds 300 files; its complete local-object comparison was used instead.

## Meaning of the recommendations

KEEP is a preservation decision within this closure-only audit. It does not recommend leaving a PR open indefinitely, and unique work alone is not proof that the whole PR should merge. A delivery assessment must choose between qualifying and merging the PR, retaining selected changes in a focused replacement and closing the original, or explicitly retiring the work and closing it. That assessment was not performed here.

For #14 specifically, the missing Privacy implementation proves that closure would abandon unlanded work; it does not establish that its bundled commercial documents, refactors and GDPR implementation should all land together. For the dependency groups #28-#30, distinct unapplied version edits establish non-duplication; compatibility and CI qualification still decide delivery.

## Inventory and recommendations

Check legend: **Green** = backend/frontend/ci-complete all success. **Red** = backend and ci-complete failure, frontend success. **Mixed red** = backend failure plus cancelled entries, frontend success plus cancelled entries, ci-complete failure. All statuses below are the captured current-head rollup; draft and KEEP do not imply merge readiness. Dependency PRs additionally have a successful Renovate status except #22, which is pending.

| PR / exact title | Head branch @ SHA | Base | Draft | Checks | Decision | Reason and preservation condition |
|---|---|---|---|---|---|---|
| [#14](https://github.com/Concertable/b2b/pull/14) Port the stranded monorepo backlog into b2b, and fix GDPR erasure | `Feature/MonorepoBacklogPort@f8e709536b3d62aa9cc362e73aa2d2076fc7e0d8` | `main` | No | Green | **KEEP** | Unique GDPR module, erasure retry/fan-out fixes, obligation gates and export readers are absent from main and #42. Preserve until that work and its tests/migrations are delivered or explicitly retired. |
| [#18](https://github.com/Concertable/b2b/pull/18) Reach shared resources through access grants instead of a venue/artist pair | `Refactor/PartyFoundationLegacyBindings@0edbbf200aacca9452ea2dc28da69ffc6e303332` | `main` | No | Green | **HOLD** | Recovery source for the replacement stack. Conditional future closure only after residual changes are accounted for and the qualified replacement lands; preserve branch/worktree. |
| [#22](https://github.com/Concertable/b2b/pull/22) Update docker/login-action digest to c94ce9f | `renovate/github-actions@b1440c7f76b09c22b69a4f24e32a1f344fd9f2be` | `main` | No | Red | **KEEP** | Unique docker/login-action digest in e2e.yml. Preserve until that exact update or a qualified newer replacement lands. |
| [#23](https://github.com/Concertable/b2b/pull/23) Update dependency Dapper to 2.1.89 | `renovate/dapper-2.x@2ce8ef57adeda37630df702b2398157d91dee66b` | `main` | No | Red | **KEEP** | Unique Dapper 2.1.72 -> 2.1.89 update. Preserve until delivered or superseded by a qualified newer update. |
| [#25](https://github.com/Concertable/b2b/pull/25) Update dependency Meziantou.Analyzer to 3.0.290 | `renovate/meziantou.analyzer-3.x@fbb30eb8317bd2f9fab2806c529ec554159a69b5` | `main` | No | Red | **KEEP** | Unique Meziantou.Analyzer 3.0.98 -> 3.0.290 update. Preserve until delivered or superseded. |
| [#26](https://github.com/Concertable/b2b/pull/26) Update dependency PdfPig to 0.1.16 | `renovate/pdfpig-0.x@84a64f2de19da3f08a6e2da1d3cdd0153b8eab2e` | `main` | No | Red | **KEEP** | Unique PdfPig 0.1.15 -> 0.1.16 update. Preserve until delivered or superseded. |
| [#28](https://github.com/Concertable/b2b/pull/28) Update dotnet monorepo | `renovate/dotnet-monorepo@159a15c20e1ec90a0030c34b5ac4998d20817340` | `main` | No | Red | **KEEP** | Unique dotnet-ef/EF 10.0.12 plus Microsoft package updates; 16 added version lines. Preserve the whole update set until delivered or superseded. |
| [#29](https://github.com/Concertable/b2b/pull/29) Update aspire monorepo to 13.5.4 | `renovate/aspire-monorepo@38e76ddeb96372515018158b98a67fcab7055ddd` | `main` | No | Red | **KEEP** | Unique Aspire 13.5.4 updates across central packages and both AppHost SDK references. Preserve all eight version changes until delivered or superseded. |
| [#30](https://github.com/Concertable/b2b/pull/30) Update azure-functions-dotnet-worker monorepo | `renovate/azure-functions-dotnet-worker-monorepo@8534980b793c689273408246de1a368cac0fd19e` | `main` | No | Red | **KEEP** | Unique four-entry Azure Functions Worker update. Preserve the full set until delivered or superseded. |
| [#31](https://github.com/Concertable/b2b/pull/31) Update dependency FluentValidation to 11.12.0 | `renovate/fluentvalidation-11.x@910510247c61ffac9274a2ad471583f2d5ca0a9e` | `main` | No | Red | **KEEP** | FluentValidation core 11.12.0 only; #33 updates a different package. Preserve this core version change until delivered or replaced. |
| [#33](https://github.com/Concertable/b2b/pull/33) Update dependency FluentValidation.DependencyInjectionExtensions to 11.12.0 | `renovate/fluentvalidation.dependencyinjectionextensions-11.x@8941a5681e5ad3c9b573f6543548f37541656fbd` | `main` | No | Red | **KEEP** | FluentValidation.DependencyInjectionExtensions 11.12.0 only; #31 does not contain it. Preserve this extension update until delivered or replaced. |
| [#34](https://github.com/Concertable/b2b/pull/34) Update dependency Microsoft.Playwright to 1.63.0 | `renovate/playwright-dotnet-monorepo@8131507216af7f1e579f30a58f7e0a5136fe3552` | `main` | No | Red | **KEEP** | Unique Microsoft.Playwright 1.59.0 -> 1.63.0 update. Preserve until delivered or superseded. |
| [#38](https://github.com/Concertable/b2b/pull/38) Scope Application and Booking access with resource grants | `Refactor/PartyFoundationApplicationBooking@3813089d16ec4ea3e8d908c1a2db2097a5ebedf1` | `main` | Yes | Red | **KEEP** | Active Application/Booking grants layer above main; 194 changed paths. Preserve owning PR and parent relationship until landed. |
| [#39](https://github.com/Concertable/b2b/pull/39) Move conversations to explicit grants and mailbox | `Refactor/PartyFoundationConversations@2eca3c77e8d1b44ff3bae0b790fc3035d59ab8bf` | `Refactor/PartyFoundationApplicationBooking` | Yes | Red | **KEEP** | Active Conversations/mailbox layer above #38; 127 changed paths. Preserve owning PR and parent relationship until landed. |
| [#40](https://github.com/Concertable/b2b/pull/40) Add business mobile mailbox preview | `Refactor/PartyFoundationMobileMailbox@218705276408478c6db13807bae6c7751cd9bc9a` | `Refactor/PartyFoundationConversations` | Yes | Mixed red | **KEEP** | Active mobile mailbox layer above #39; 16 changed paths. Preserve owning PR and parent relationship until landed. |
| [#41](https://github.com/Concertable/b2b/pull/41) Restore Party Foundation E2E reset and route proof | `Refactor/PartyFoundationE2EProof@19e3e177858ce315531e919759199d231a0d71b0` | `Refactor/PartyFoundationMobileMailbox` | Yes | Mixed red | **KEEP** | Active E2E reset/routes layer above #40; 27 changed paths. Preserve owning PR and parent relationship until landed. |
| [#42](https://github.com/Concertable/b2b/pull/42) Apply composable tenant roles and shared resource authorization | `Refactor/PartyFoundationFinalReconciliation@262a086b47526b07ecac96158c712c8de0dd026c` | `Refactor/PartyFoundationE2EProof` | Yes | Red | **KEEP** | Active roles/RBAC and lifecycle reconciliation layer above #41; 337 changed paths. Its owner retains repair/qualification responsibility; preserve until landed. |

## Priority closure shortlist and dependencies

1. **#18 â€” conditional future candidate; HOLD today.** Retain the green recovery source until the replacement stack is qualified and landed. Before a later explicit closure decision, account for every meaningful residual from #18, including security behavior, tests, migrations, configuration and recorded design/recovery material. Preserve any unique work in its owning delivered change, or record an explicit retirement decision. Closing the PR never authorizes deleting its branch or worktree.
2. **#14 â€” KEEP, excluded from the closure shortlist.** Preserve the stranded backlog and GDPR repairs. It is not a duplicate of the Party Foundation stack. Delivery or explicit retirement of its unique work must precede reconsideration.
3. **Renovate PRs â€” KEEP, excluded from the closure shortlist.** No duplicate update was found. #31 and #33 may be reviewed together because they target the same release family, but their diffs change distinct package entries. Closing either as part of consolidation would first require a qualified replacement that contains its exact update.

Recommended order: qualify and land the existing dependency chain **main -> #38 -> #39 -> #40 -> #41 -> #42**, preserving/rebasing children through normal delivery; finish the residual #18 preservation check; only then consider explicitly authorized closure of #18. #14 has no proven supersession dependency on that landing and remains open independently. Dependency updates have no demonstrated closure prerequisite on the feature stack. This audit authorizes none of those delivery or closure actions.

## #14: direct evidence of unique work

Against its merge base `de2477327f6578f11706ffad30a1ff7face0466c`, #14 changes **214 paths**. All **23 non-merge commits** are unmatched by `git cherry` against both current main and #42. A lack of patch matches alone does not prove uniqueness, but the target trees also lack the **entire 60-path Privacy module**, `DeferredErasureSweepFunction.cs`, Application/Booking/Concert obligation checkers, Booking contract export readers and Conversations erasure/export services. Searches of both exact target trees found none of `RequestErasureAsync`, `DeferredErasureRunner`, `CaptureFanOutState` or `SubjectRightsController`.

Read the final #14 erasure service and runner, rather than relying on its older PR description: they reuse a request by subject ID, retain completed requests, retry resumable work, capture fan-out state before scrubbing, purge invitations and sever messages/profiles, and perform user erasure. Their associated Privacy tests and migrations are absent from the comparison targets. Commercial/GDPR plan artifacts rescued by #14 also remain missing at their original paths. Some smaller refactors may overlap later work, which does not justify closing the unique module.

Changed-path end-state counts: against main, 0 identical / 76 different / 132 absent in target / 6 retained in target despite deletion in #14; against #42, 7 identical / 74 different / 127 absent / 6 retained. These count original paths, so renamed files can fall into the absent bucket. No claim that all 214 paths are unique is made.

## #18: replacement evidence and remaining proof

Against its merge base with current main, `61f91a7143007ab3a43e99fb2626820589f4994d`, #18 changes **845 paths**. Of those path end states, **553 match #42**, **239 differ**, and **53 are absent under their original paths**. Matching end states include paths deleted in both trees. Against main the counts are 474 matching / 246 differing / 93 absent / 32 retained despite #18 deletion. Its **135 non-merge commits** have no patch-equivalent matches against either target; the replacement includes restructuring, so neither commit ancestry nor patch equivalence proves complete absorption.

Concrete replacement evidence includes rename detection from `CommandExecutor` to `TransactionRunner`, command-facts interfaces to privileged read repositories, `MembershipAuthorityFence` to `MembershipResolver`, and the mobile/web Concert naming corrections. Four Application/Booking/Concert/Conversations initial migration bodies match under new filenames; Tenant migration content changes with roles. Selected regressions also survive: the replacement `TransactionRunnerTests` retains lost-commit-acknowledgement assertions for one execution/one committed row, and `CommandPayloadHashTests` retains separator/null collision, culture and DateTime checks from the old receipt tests.

Those samples support replacement intent, not full equivalence. The remaining evidence needed before closure is a disposition of all **239 differing and 53 absent original paths**, accounting for actual renames and deliberate replacements, with special attention to:

- Tenant/Authorization policy and grant semantics, changed migrations, and the privilege/transaction boundaries.
- Changed Application, Booking, Concert, Conversation and Opportunity security/race regressions, plus CI/build/package configuration.
- The original design, progress and review artifacts removed from the published replacement tree; preserve the recovery source until their necessary content has a durable owner.

The published #42 aggregate checks are red; its separate owner is handling the regression. #38/#39 are also red, and #40/#41 have mixed failed/cancelled rollups. The captured green #18 status does not remove the replacement qualification/landing gate. All five published parent heads are ancestors of their child heads; the stack is real dependent work, not five duplicates of the tip.

## Dependency overlap evidence

The ten captured patches add **35 version/digest lines**. None is present in either exact main or #42; no two PRs update the same package key, SDK declaration or action line. Most share `Directory.Packages.props`, which is file overlap rather than duplicate work. #28 also changes dotnet-ef; #29 also changes both AppHost SDK references; #22 changes only the E2E docker login digest. #31 changes `FluentValidation`; #33 changes `FluentValidation.DependencyInjectionExtensions`.

Their red checks still require owner qualification. This bounded audit did not assess release compatibility, diagnose each failure, or run upgrades; KEEP means preserve useful updates, not approve merging them.

## Completion and remaining unknowns

Audit complete: all 17 PRs have a recorded current head/base/check snapshot, recommendation and preservation condition. There is no PR recommended for immediate closure. The remaining #18 equivalence/qualification gate, #14 integration with the evolving foundation, and dependency compatibility/CI belong to later delivery decisions. No external state was mutated.


## Delivery outcomes after authorization

The user subsequently authorized delivery and cleanup, superseding the audit-only KEEP/HOLD recommendations for #14 and the ten dependency PRs.

| PRs | Observed outcome | Evidence / remaining work |
|---|---|---|
| #28 | MERGED | Reviewed head b8161c62df86c120c5a3d67a41ce31eeb9572180; main merge 84b7d89641aad0ca6e0273af3794234a7fd934af; causal main CI 37028717117 passed. .NET package/tool updates plus actual worker OpenSSL repair; scans retained. |
| #22/#23/#25/#26/#30/#31/#33/#34 | CLOSED | All original update lines preserved in published #29, with authored cherry-pick history and retained source branches. Exact proof: pr29-preservation-proof.json. |
| #29 | MERGED; main CI passed | Reviewed head 6377f643800bc50be11937bd00baab896188d8c6; merge d8bec353bc55db0c919134876522bd522caa0210 at 18:10:37 UTC; causal main CI 37045670696 passed. All nine original update sets preserved, including Functions compatibility alignment. |
| #14 | Export-only salvage in progress; unpublished | Counterparty names excluded as confirmed by the user. Erasure retired after last-owner and membership/obligation races; original and repaired work preserved in existing history. Fresh export qualification, review and remote CI required. |
| #18/#38–#42 | Separate owner preserved | Recovery PR and active foundation stack are outside this delivery scope. No branch/worktree deletion. |
