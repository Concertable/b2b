# Code review — Feature/MonorepoBacklogPort

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `9c9908c3c9d9e032cb433c78528b8492c56f2484`
**Security-reviewed up to commit:** `9c9908c3c9d9e032cb433c78528b8492c56f2484`
**Judgment:** `changes-requested`

## Review pass — 2026-09-15 — full

**Candidate base:** `2b5264b4c9748931c840c816e99408d537bec7c2`
**Candidate head:** `c4e1e88e3d2c181410c4b695e09deb141465f8aa`
**Candidate branch:** `Feature/MonorepoBacklogPort`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:a03090507d1abae6bc2840f98e3a5fd01c45c41ad5acf2e80352b41eab3acbee` `(173 paths)`
**Candidate bundle:** `C:\Users\TOMMYS~1\AppData\Local\Temp\claude\C--Users-TommySeery-source-repos-Concertable-b2b\5646f96d-1fec-4dd0-99ab-f200d2a701cd\scratchpad\review-bundle`
**Candidate bundle identity:** `sha256:f4149a7fcd64d83dd464d79b3f6883dd264d54ad33c9df78438a4c4f46a9063d`
**Work-order path:** `reviews/Feature-MonorepoBacklogPort.md`
**Work-order mode:** `new`
**Pass judgment:** `changes-requested`

Rules were resolved manually: this repository carries no `.agents/hooks/skill_router.py` in its frozen
tree, a gap its own `TECH_DEBT.md` records. Premise read from the frozen tree's root `AGENTS.md`,
`CODE_PATTERNS.md`, `ARCHITECTURE.md` and `api/src/Modules/Concert/AGENTS.md`. Layers: native/general,
module-boundary + persistence + multitenancy + seeding, failure-carrier + conventions, changed-behaviour
test impact. Security classification qualified on the changed paths (new authenticated endpoint,
authorization policy, rate-limit policy, PII erasure and disclosure); that evidence is folded into F2, F10
and F13 rather than carried separately. Every finding below was confirmed by the parent against the frozen
tree before being kept.

### Findings

- [x] **F1 — CRITICAL — correctness** — `api/src/Modules/Booking/Concertable.B2B.Booking.Infrastructure/Services/ObligationChecker.cs:13`, `api/src/Modules/Application/Concertable.B2B.Application.Infrastructure/Services/ObligationChecker.cs:13`
  Erasure can never complete for any subject with booking history. `SettledStates` omits `BookingState.Confirmed` and `ApplicationState.Accepted`, but `BookingStateMachine.cs` gives `Confirmed` no outgoing edge and `ApplicationStateMachine.cs` gives `Accepted` none — both are terminal, so both count as a live obligation forever. A booking confirmed in 2024 whose concert completed and invoiced still defers the DSAR, and the hourly sweep re-defers it every hour indefinitely. Two of the three gates can never open. Fix: count only genuinely in-flight states, and let the Concert checker — which has a real terminal `Complete` — own post-confirmation settlement.

- [x] **F2 — CRITICAL — security** — `api/src/Modules/Conversations/Concertable.B2B.Conversations.Domain/Entities/MessageEntity.cs:62`
  The export re-assembles every erased subject's messages. `SeverAuthor()` sets `SentByUserId = Guid.Empty`, and `MessagePrivilegedRepository.cs:16-17` matches that column by equality over the unfiltered privileged context. `GET /api/subject-export/00000000-0000-0000-0000-000000000000` satisfies the `{subjectId:guid}` constraint and returns the retained message bodies of every previously-erased subject across every tenant, in one download — defeating the sever it exists to complete. Fix: make `SentByUserId` nullable and sever to `null`, or reject the empty GUID at both service entry points.

- [x] **F3 — CRITICAL — correctness** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectErasureService.cs:93`
  A re-driven erasure permanently skips the profile scrub and the invitation purge. The wound-down tenant list is the return of `SeverMembershipsAsync` and the email is read from the user row before tombstoning. On a second pass the memberships are already gone, so the return is empty and `ConversationsErasureService.cs:33-34` short-circuits; the email is already tombstoned, so no invitation can match. Making erasure re-drivable without making the fan-out idempotent *as composed* means a resumed run silently leaves `ParticipantProfile` rows — a sole trader's name and address — and pending invitations intact, with no signal. Fix: capture the wound-down tenant set and the subject's email onto the request row on the first pass and drive later passes from that durable state.

- [x] **F4 — HIGH — correctness** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Repositories/SubjectErasureRepository.cs:12`
  A crash mid fan-out strands the subject in `InProgress`, invisible to the sweep. `DriveAsync` commits `InProgress` before the non-atomic five-call fan-out, and `ListDeferredAsync` lists only `Deferred`, so the `(InProgress, Begin)` edge added for exactly this case has no caller. `ErasureTrigger.Fail`, `RecordFailure` and `ErasureState.Failed` have no production caller anywhere, and `Failed` has no outgoing edge, so it is a trap state the moment it becomes reachable. Fix: widen the work list to include `InProgress`; wrap the fan-out so a hard error fires `Fail` plus `RecordFailure` and persists; add a `Failed` recovery edge.

- [x] **F5 — HIGH — module boundaries** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/DeferredErasureRunner.cs:23`
  The sweep runs every subject through one shared DI scope, so a subject whose `SaveChangesAsync` throws leaves dirty tracked entities that the next subject's save flushes — the per-item try/catch gives isolation it does not provide. Breaks `api/src/Modules/Concert/AGENTS.md:76` ("background completion invokes the workflow directly through a fresh scope"), whose precedent is `CompletionRunner.cs:27-33` collecting ids and using `IScoped<T>`. Fix: list subject ids, inject `IScoped<ISubjectErasureService>`, re-read inside the scope.

- [x] **F6 — HIGH — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectErasureService.cs:103`
  A typed conflict is thrown away as an exception: `Advance` builds `ErasureTransitionError` then throws `InvalidOperationException` carrying only its message, discarding a `Conflict` definition. `(InProgress, Defer)` is missing from the table, so an operator re-POST after a crashed run returns an opaque 500 rather than 409. The house convention is a returned union case (`ApplicationService.cs:221-222`, `SettlementService.cs:152-153`), and `SubjectRightsController` is the only controller of 21 with no Result terminal — `SelfBillingAgreementController.cs:36-41` is the file-returning precedent. Fix: return a Result, terminate it in the controller, add `Reunion.AspNetCore` to `Privacy.Api.csproj` (the arch guard requires direct ownership), add the missing edge.

- [x] **F7 — MEDIUM — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Api/Controllers/SubjectRightsController.cs:14`
  `[Authorize(Policy = "Admin")]` duplicates the literal `AdminAttribute` owns. `ModerationController.cs:5,18` is the cross-module precedent for taking the `Admin.Api` reference and using `[Admin]`. A policy rename would not fail to compile here; it would 500 at request time.

- [x] **F8 — MEDIUM — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Api/Controllers/SubjectRightsController.cs:16`
  The only controller in the tree with no class-level `[Route]`; all 20 others have one. A later action added without an absolute template would have no route at all.

- [x] **F9 — MEDIUM — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Domain/Lifecycle/ErasureStateMachine.cs:7`
  Hand-rolls what the kernel `StateMachine` provides and is DI-registered, where `BookingStateMachine`, `ApplicationStateMachine` and `ConcertStateMachine` are one-line derivations held static on their entity. `SubjectErasureRequestEntity.Transition` is a state setter with no guard, so the aggregate's state can be set without consulting its own machine.

- [x] **F10 — MEDIUM — docs** — `CODE_PATTERNS.md`, `ARCHITECTURE.md`
  Neither is updated. Privacy is a fifth untenanted DbContext absent from the stance roster, and `ConversationsPrivilegedDbContext` is now used for GDPR erasure while its own doc and the roster still say moderation only. Root `AGENTS.md` names `CODE_PATTERNS.md` as *the* stance roster, so a stale roster is the mechanism by which the next change picks the wrong stance.

- [x] **F11 — MEDIUM — module boundaries** — `api/tests/Concertable.B2B.IntegrationTests.Fixtures/PrivacyApiFixture.cs:3`
  Sits in the shared cross-module fixtures project; all 13 siblings live in their own module suite. Every module's integration suite now compiles against a Privacy-specific type.

- [x] **F12 — MEDIUM — module boundaries** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Application/DTOs/SubjectErasureRequestDto.cs:5`
  `public` where sibling Application DTOs are `internal`, and `FileDownload.cs:3` beside it is internal. The `InternalsVisibleTo` chain already covers its only consumer. Privacy has no Contracts project, so this is the whole public surface of an Application assembly that Workers also references.

- [x] **F13 — MEDIUM — product decision** — `api/src/Modules/Booking/Concertable.B2B.Booking.Contracts/IBookingModule.cs:29`
  The export returns tenant-level records for every tenant the subject belongs to, including `SubjectContractDto.ArtistName` — a named individual when the counterparty is a sole trader. UK GDPR art. 15(4) says access must not adversely affect others' rights, and these are the tenant's records rather than the subject's personal data. No loaded doc rules either way; this needs an owner decision, not a silent default.

- [x] **F14 — MEDIUM — efficiency** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectObligationChecker.cs:29`
  `GetLiveObligationCountAsync` returns a count no caller reads as a number — it is only compared against zero. Three counting scans per check where an existence check short-circuits on the first row.

- [x] **F15 — MEDIUM — efficiency** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Repositories/SubjectErasureRepository.cs:12`
  The sweep's work list is unbounded: no cap, processed serially, four queries per subject. With F1 in place deferred rows only accumulate, so each hourly pass costs strictly more than the last.

- [x] **F16 — MEDIUM — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Domain/Lifecycle/ErasureTransitionError.cs:15`
  Error code uses `invalid_transition` where all seven existing transition errors use the `invalid_state` segment.

- [x] **F17 — MEDIUM — test coverage** — `api/src/Modules/Privacy/Tests/Concertable.B2B.Privacy.UnitTests/ErasureTransitionErrorTests.cs:14`
  Asserts `Contains` on the message and never asserts the error kind, so the 409 could silently become a 422. `ErrorDefinitionContractTests.cs:180-193` pins code, exact message and kind for all 24 Concert cases.

- [x] **F18 — MEDIUM — test coverage** — `api/src/Modules/Privacy/Tests/`
  Ranked gaps: no `DeferredErasureRunnerTests` at all; nothing anywhere exercises a real `SettledStates` list, which is exactly why F1 shipped with a green suite; no `SubjectExporterTests`; `ScrubParticipantProfilesAsync` and `PurgePendingInvitationsAsync` are asserted nowhere in the tree; `SubjectRightsApiTests` has never executed and no seeded subject has exactly one class of obligation, so it cannot isolate a regressed checker.

- [x] **F19 — LOW — docs** — `api/src/Modules/Concert/Concertable.B2B.Concert.Application/Interfaces/ISubjectRecordReader.cs:5`
  Doc claims it returns contracts; those come from Booking.

- [x] **F20 — LOW — correctness** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectErasureService.cs:109`
  Discards the `TryGetValue` bool, so a union extension would silently yield `default` = `Requested`, rewinding a live request. Masked today only by the throw that F6 removes.

- [x] **F21 — LOW — conventions** — `api/src/Modules/Application/Concertable.B2B.Application.Contracts/IApplicationModule.cs:21`
  The tenant-id facade members do not name their key, where `IBookingModule.GetByApplicationIdsAsync` does. Extends a stated repository rule to a facade by analogy — the weakest finding here.

### Dropped after parent verification

- **Email-normalisation asymmetry** (raised as uncertain): invitations are stored normalised — `InvitationService.cs:57` trims and lower-cases on create and `TenantInvitationEntity.cs:12` documents it; the purge normalises before comparing. Not a defect.
- **The `ActionLink` dedup bundled into a GDPR branch**: scope observation, not a defect, and it is its own commit.
- **Style items** (primary-constructor mix, static-readonly casing): already mixed in the pre-existing tree, so preference rather than convention.

### Verdict

F1 alone means the feature does not work: it builds, and all 28 Privacy unit tests pass, because every test
touching the obligation gate mocks it. F2 is a disclosure defect on the very endpoint meant to honour
erasure. F3, F4 and F5 are regressions introduced by the re-drive change in `c4e1e88e` itself — the fix for
the original one-shot bug brought its own.

## Review pass — 2026-09-17 — incremental

**Candidate base:** `c4e1e88e3d2c181410c4b695e09deb141465f8aa`
**Candidate head:** `514894d6` *(see top-level watermark)*
**Candidate branch:** `Feature/MonorepoBacklogPort`
**Candidate scope:** `all` `(51 paths)`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

Covers `d2613b7d`, `9a129bb5`, `ca4b8bc6`, `b4fc79c1`, `d17013db` — the checker verb change, the
`IReadOnlySet` retyping, the `*Export` → `Subject*Dto` rename, the `DealType` enum switch and the
Conversations reader split. Lenses: native/general, module-boundaries + conventions, changed-behaviour
test impact. Rules resolved manually again — the frozen tree still carries no `.agents/hooks/skill_router.py`.

Findings F22–F24 were raised and fixed inside this pass (`514894d6`) and are recorded for history.

### Findings

- [x] **F22 — HIGH — correctness** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectExporter.cs`
  The `DealType` `string`→enum switch silently changed the GDPR portability artifact. `JsonSerializerDefaults.Web` supplies camelCase naming and case-insensitive reads but **no** `JsonStringEnumConverter`, and `DealType` carries no `[JsonConverter]`, so `"dealType": "FlatFee"` became `"dealType": 0`. Enum members are implicitly ordinal, so inserting a `DealType` value would re-map the meaning of every previously issued export. Fixed: options extracted to `SubjectExportSerializerOptions` with the converter, pinned by `SubjectExportWireFormatTests`.

- [x] **F23 — MEDIUM — correctness** — `api/tests/Concertable.B2B.ArchitectureTests/ModuleBoundaryTests.cs`
  Two defects in the guard `d2613b7d` widened. Bare `StartsWith` meant `IssueInvoiceAsync` — an unambiguous command — satisfied the `Is` prefix, so the test that exists to reject commands admitted them. And the sibling `LaterLifecycleStages_DoNotCommandAnEarlierStage` still classified by `"Get"` alone, so `HasLiveObligationsAsync` was a query to one guard and a command to the other; it passed only because Privacy, not a lifecycle stage, is its sole caller. Fixed: word-boundary match, both guards driven from one `QueryPrefixes`.

- [x] **F24 — LOW — docs** — `api/src/Modules/Concert/Concertable.B2B.Concert.Application/Interfaces/ISubjectRecordReader.cs:5`
  Doc claimed invoices, **contracts** and self-billing agreements; the reader queries only invoices and agreements, and contracts come from Booking's `SubjectContractReader`. This is F19's underlying defect, carried through the rename. Fixed; F19 ticked.

- [x] **F25 — HIGH — module boundaries** — `api/src/Modules/Conversations/Concertable.B2B.Conversations.Infrastructure/Services/SubjectMessageReader.cs:7`
  The extracted read-only reader is bound to `IMessagePrivilegedRepository`, the **writable** cross-tenant privileged context. `CODE_PATTERNS.md:29-31` allows `XPrivilegedRepository` "only where a cross-tenant write flow exists", and this reader has none — its two siblings created in the same commit use their module's read stance (`IBookingReadDbContext`, `IConcertReadDbContext`). Conversations has no read stance to use: only `ConversationsDbContext` and `ConversationsPrivilegedDbContext` exist, and `CODE_PATTERNS.md:15` omits Conversations from the read-context roster. Inherited from `ConversationsErasureService` rather than introduced, but the extraction was the moment to move the stance. Fix: add `IConversationsReadDbContext` (unfiltered read-only — the subject's messages genuinely cross tenants) plus an `IMessageReadRepository`, bind the reader to it, and add Conversations to the roster.

- [x] **F26 — HIGH — test coverage** — `api/src/Modules/Conversations/Tests/`, `api/src/Modules/{Booking,Application,User,Venue}/Tests/*.UnitTests`, `api/src/Concertable.B2B.DataAccess/Tests/`
  Six test projects carry no `[assembly: AssemblyTrait("Category", …)]`, and `.github/workflows/ci.yml:73-74` filters on `Category=Unit|Integration|Architecture|Startup`. A test added to any of them compiles and is then skipped by the gate. Both Conversations projects are in that set, which is why the reader split could not be covered where it belongs. Fix: add an `AssemblyInfo.cs` to each, copying `Booking.IntegrationTests/AssemblyInfo.cs:3`.

- [x] **F27 — MEDIUM — test coverage (closed by CI 35255490459)** — `api/src/Modules/Booking/Tests/Concertable.B2B.Booking.IntegrationTests/`
  Nothing has ever executed `IReadOnlySet<Guid>.Contains` inside an EF query against the real provider. `IReadOnlySet<T>.Contains` is a different expression-tree method from `ICollection<T>.Contains`; whether it emits `IN` is decided at runtime by SQL Server. The one in-tree precedent (`MessageRepository.cs:71`) ships but sits behind the same never-executed suite, and the only EF unit test in Conversations uses `UseInMemoryDatabase`, which evaluates client-side and would not expose a translation failure. Six queries share the construct, so they fail together or not at all. Fix: one real-provider call to `IBookingModule.GetSubjectContractsAsync(new HashSet<Guid> { … })` in `TenantScopingTests`.

- [x] **F28 — MEDIUM — test coverage** — `api/src/Modules/Privacy/Tests/Concertable.B2B.Privacy.IntegrationTests/SubjectRightsApiTests.cs`
  `ToNullable()`'s `None` branch is asserted nowhere; if it yielded anything but `null` the export would emit a phantom user fragment unnoticed. One test exporting an unknown subject id asserts `JsonValueKind.Null` for `user` and simultaneously becomes the only coverage of every empty-set early return this pass added. Highest value per test in the set.

- [x] **F29 — MEDIUM — correctness** — `api/src/Modules/Tenant/Concertable.B2B.Tenant.IntegrationTests/`
  `SeverMembershipsAsync`'s return is asserted nowhere, and no test subject belongs to two tenants, so the wound-down set — the value that drives `ScrubParticipantProfilesAsync` — is unexercised. `SubjectRightsApiTests` asserts only the severing side effect.

### Notes carried, not raised as findings

- `Concert.Contracts` → `Deal.Contracts` **is not** a boundary violation: `Directory.Build.targets:41-48` flags only paths escaping the service root, `Booking.Contracts` already carries the identical reference, `Deal.Contracts` is `IsPackable` and listed in `.github/b2b-promotion-candidates.json`, and `MODULES.md:20` permits Contracts→Contracts for shared base types. Conclusive.
- `ToNullable` is Reunion-provided (`Reunion.OptionReferenceNullableExtensions`), so `CARRIERS.md:238`'s ban on *local* nullable helpers does not apply.
- `StartupTests.ResourceGraphTests.ProductionGraphAndStrictValidation_AreValid` fails locally with a `TimeoutException` inside `Concertable.Payment.Hosting.AppHostExtensions.AddStripeCli`. No path in this range touches AppHost/hosting/Aspire and the suite predates the branch, so it is not attributed to this candidate — but it is unverified, not green.
- Removing the `SettledStates` comments was requested and is compliant with the zero-comment default; the invariant they stated belongs in the test F18 already asks for.

## Remediation — 2026-09-17

Twenty of the twenty-four findings are closed across `67f3b80b`, `88446a14`, `2b85f6a6` and `66659334`.
Validation at each step: build 0 errors; Unit + Architecture through CI's own filter 543 passed / 0 failed
across all fourteen projects, including the six the missing `Category` traits had been hiding. StartupTests
is 14/15 — the one failure is `ResourceGraphTests.ProductionGraphAndStrictValidation_AreValid` timing out
inside `Concertable.Payment.Hosting.AppHostExtensions.AddStripeCli`, which no changed path can reach and
which fails identically before these commits.

### Open, with their durable owner

- **F1 — Booking half — CRITICAL — needs an owner decision.** The Application half is closed: `Accepted` is
  settled because `ApplicationAcceptedDomainEventHandler` is an `IPreCommitDomainEventHandler`, so the
  Booking row commits in the same transaction and Booking's gate provably owns it. `BookingState.Confirmed`
  is **not** the same shape — `BookingConfirmedIntegrationEventHandler` is an `IIntegrationEventHandler`, so
  the Concert row arrives asynchronously. Classifying `Confirmed` as settled opens a window in which neither
  gate reports an obligation and an erasure sweep can complete against a concert about to exist, trading a
  gate that never opens for one that opens too early on a one-way operation. Resolution requires choosing
  between: Booking staying live until it records the handoff acknowledgement, or erasure refusing while the
  subject has undelivered handoff messages. **Owner: Tommy.**

- **F13 — product decision — unchanged.** Whether `SubjectContractDto.ArtistName` — a named individual when
  the counterparty is a sole trader — belongs in another tenant's export under UK GDPR art. 15(4). No loaded
  doc rules either way. **Owner: Tommy.**

- **F18 — partially closed.** Landed: the `SettledStates` exhaustiveness guard (`ApplicationObligationTests`),
  which is the specific gap that let F1 ship green, plus `SubjectExportWireFormatTests`, the wound-down-set
  test and the unknown-subject export test. Still open: `DeferredErasureRunnerTests`, and assertions for
  `ScrubParticipantProfilesAsync` / `PurgePendingInvitationsAsync`.

- **F27 — deliberately not closed locally.** It asks for one real-provider execution of
  `IReadOnlySet<Guid>.Contains` inside an EF query. That needs Docker and the integration tier, which the
  repository gates in the merge queue rather than on the workstation. Closing it from a local run would be a
  claim the evidence does not support. Resolution condition: a green `Category=Integration` run covering
  `GetSubjectContractsAsync`.

### Remediation closeout — 2026-09-17

Twenty-three of twenty-four findings closed. `19a9040a` closed F1's remaining Booking half and `bc51d670`
closed F18 and wrote F27's tests.

**F1 is fully closed.** The Booking half needed a mechanism rather than a decision, and the safe option was
the only correct one: a `Confirmed` booking now settles when — and only when — Concert acknowledges the
handoff. Booking subscribes to `ConcertCreatedEvent` (which already carries the `ApplicationId` Booking keys
on) and records `HandedOffAtUtc`; `BookingObligation` treats `Confirmed` as settled only once that timestamp
exists. The migration backfills from the concert schema, without which every historical `Confirmed` booking
would have read as live and the fix would have reintroduced the bug.

- [x] **F27 — closed by CI run 35255490459.** `TenantScopingTests` now carries two real-provider assertions
  that `IReadOnlySet<Guid>.Contains` translates to SQL. They cannot run on this workstation — Docker plus the
  integration tier — so the merge queue closes this, not a local run. Resolution condition unchanged: a green
  `Category=Integration` pass over `GetSubjectContractsAsync` and `HasLiveObligationsByTenantIdsAsync`.

- [x] **F13 — still the owner's.** Whether `SubjectContractDto.ArtistName` — a named individual when the
  counterparty is a sole trader — belongs in another tenant's export under UK GDPR art. 15(4). This is a
  product and legal judgement, not an engineering one, and no loaded doc rules either way. **Owner: Tommy.**

### F27 closed — CI run 35255490459 on `e635fc19`

`Concertable.B2B.Booking.IntegrationTests` 26/26 against real SQL Server, covering both set-translation
assertions: `IReadOnlySet<Guid>.Contains` does emit `IN`, so all six queries retyped on this branch are safe.
`Concertable.B2B.Privacy.IntegrationTests` 5/5 — the first execution of that suite anywhere, which the repo's
own progress ledger had recorded as never having run.

That leaves **F13** as the single open finding, and it is a product/legal judgement rather than a defect.


## Review pass — 2026-10-02 — reconciled subject rights

**Candidate base:** `84b7d89641aad0ca6e0273af3794234a7fd934af`
**Candidate head:** `f1040fb2e7503d8c919a4b7b38207f6a3df4fec3`
**Candidate branch:** `Feature/MonorepoBacklogPort`
**Candidate scope:** `all`
**Candidate path-set:** `c3721f2fbc58ec630f4de9490230898434cf87233fb65a90bcddb0073d4ff457`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\pr-disposition-20261002\review\0f47f5e3642c620cc92db01d557f89e19719d1a0db07c812ebc7877e1ca7002f`
**Candidate bundle identity:** `9c35f6e8f84836b8dc7c967ac382945fa739cdb910ba027007dfcabdaf98603e`
**Candidate patch identity:** `627a75828221d9787d46d8f0827c801537e2b45839045f26be84c8904508d851`
**Work-order path:** `reviews/Feature-MonorepoBacklogPort.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

Synchronized tracked-clean source with origin/main once through native Git. Untracked audit Markdown was preserved; runtime synchronization had rejected those working documents before any source operation. Frozen descriptor: C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\pr-disposition-20261002\review\0f47f5e3642c620cc92db01d557f89e19719d1a0db07c812ebc7877e1ca7002f\descriptor.json

Native Codex CLI unavailable; configured native-general review model unavailable. Parent fallback and fresh independent read-only contexts cover the immutable range. Full ordinary pass covers the coherent 160-path capability; source contracts, persistence/migrations and security/recovery concerns receive separate bounded lenses. No repository route table; .NET tier rules and root/nearest guidance apply. Security is required by actual admin subject export and destructive PII erasure even though the helper generic substring classifier reports no qualifying path.

Historical F13: conservative source repair removes VenueName and ArtistName from contract exports; wire-format regression forbids both. Final judgment remains pending. Crash retry repair persists tenant IDs before membership removal and clears completed journal PII; focused Tenant provider replay passed 1/1, Privacy unit regressions passed 38/38. New PostgreSQL migrations regenerated through repository owner helper.


### Findings in reconciled candidate

- [x] **F14 — HIGH — financial obligation guard** — `api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Services/ObligationChecker.cs:12` treats Draft as settled. A confirmed booking hands off to a draft concert before financial completion; the booking checker then excludes it, allowing erasure while settlement is still due. Fix: keep Draft live and verify the handoff boundary.
- [x] **F15 — HIGH — production migrations** — `migrations.psd1:9` adds Privacy to validation but the production migration executable has a separate catalog without it. Add its project reference, factory visibility and migration catalog entry; extend the clean-database/idempotence regression.
- [x] **F16 — HIGH — concurrent journal writes** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectErasureService.cs:102` persists captured PII without a concurrency token. A stale scoped attempt can write original email after another scope completed and cleared it. Add persisted compare-and-swap fencing and a separate-DbContext regression proving stale capture fails and completed PII remains null.

- [x] **F17 — HIGH — background handoff** — `api/src/Modules/Booking/Concertable.B2B.Booking.Infrastructure/Events/ConcertCreatedIntegrationEventHandler.cs:23` uses a tenant-filtered repository without an active request tenant, silently missing the booking and leaving confirmed obligations live indefinitely. Use the module privileged context and verify background delivery/replay.
- [x] **F18 — MEDIUM — retry fairness** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Repositories/SubjectErasureRepository.cs:24` always selects the oldest 100; persistent deferrals starve every newer request. Persist last-attempt time and order by oldest attempt; verify a 101st request rotates into the next batch.

The 2026-10-02 immutable pass is complete with five retained findings, deduplicated across three fresh read-only lenses and parent validation. Security was reviewed at f1040fb2e7503d8c919a4b7b38207f6a3df4fec3. Repairs are local and require a new immutable pass after regression/migration qualification.

The user explicitly confirmed counterparty exclusion. F13 is resolved by removing both VenueName and ArtistName; the wire-format regression forbids both. No policy ratification or broader GDPR-compliance claim is implied.


## Incremental review pass — erasure safety repairs — 2026-10-02

**Candidate base:** `f1040fb2e7503d8c919a4b7b38207f6a3df4fec3`
**Candidate head:** `938e9d8a25ff3b91723abdbf402057074b596a47`
**Candidate branch:** `Feature/MonorepoBacklogPort`
**Candidate scope:** `all`
**Candidate path-set:** `8cda1f06b22e8c3c7a91c9b62320b610d8d4a0ec5cb14399140d20449bcee48f`
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\pr-disposition-20261002\review\93f8c351b1ff926b403e0457be6a4f3b4c71760ed3d5581e05577d61ea73428f`
**Candidate bundle identity:** `900790b12aacbfb040090f3c12087f51e4ed4d868580a9e73c4aeef18979ba6e`
**Candidate patch identity:** `0797801604fb30bf0208bfbbe9b15445d9de753aacf554955b3bfc6210c4cefc`
**Work-order path:** `reviews/Feature-MonorepoBacklogPort.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

Delta is exactly the 23 descriptor paths after the prior completed f1040fb2 review. Native role remains unavailable; parent fallback and fresh read-only general, security/recovery and persistence/integration evidence run against the same frozen artifacts. Security examines the cumulative trunk range 84b7d89641aad0ca6e0273af3794234a7fd934af..938e9d8a25ff3b91723abdbf402057074b596a47 because destructive PII behavior qualifies despite generic helper path classification. Dotnet tier rules and nearest guidance remain unchanged. Original F14–F18 text/severity/pass identity preserved; status may resolve only after validation. Privacy units 39/39 and PostgreSQL cases 7/7 passed. Remaining provider projects still running; corrected handoff test uses existing background dispatcher.


### Final disposition of erasure safety findings

The immutable 938e9d8 safety pass is complete. Fresh general and persistence lenses found no defects in the 23-path repair delta. The fresh security lens retained three additional concerns, confirmed by the parent:

- [x] **F19 — MEDIUM — retry fairness after failed guard query** — SubjectErasureService changed LastAttemptedAtUtc in memory but saved after the obligation query; query failures left the oldest 100 eligible forever. Retired with the erasure service and runner; no retry path lands.
- [x] **F20 — HIGH — last-owner invariant** — TenantErasureService deleted a sole Owner from a shared tenant while non-owner members survived, bypassing MembershipService's last-owner protection. Retired with the destructive membership facade; ordinary owner protection remains unchanged from main.
- [x] **F21 — HIGH — membership/obligation fan-out scope** — The service checked obligations before capture and re-read memberships during sever. Invitation acceptance could add a tenant with live obligations between guard and mutation. Journal xmin did not fence membership/financial writes. Retired with the erasure path; coordinated lifecycle fencing is required for any future implementation.

F14–F18 repairs passed 39 unit and seven Privacy provider cases, plus Booking handoff, Concert obligation and production migration provider projects. The final export salvage removes those mutation and persistence prerequisites completely, restores main's Booking/Conversations schemas and migration catalog, and preserves the repaired checkpoint in ef1868da9 history. All newly retained erasure findings are resolved for the landed scope through removal, not a claim that the preserved implementation is safe.

The final export-only candidate remains review-pending. A fresh full owning-base pass will cover the admin route, module readers, subject isolation and financial metadata with independent general and security lenses.


## Full review pass — final read-only export — 2026-10-02

**Candidate base:** `d8bec353bc55db0c919134876522bd522caa0210`
**Candidate head:** `9c9908c3c9d9e032cb433c78528b8492c56f2484`
**Candidate branch:** `Feature/MonorepoBacklogPort`
**Candidate scope:** `all`
**Candidate path-set:** `f930a6907e7c6489f5566588876231a6a85194a5181c4c838ee08194e22e2213` (72 paths)
**Candidate bundle:** `C:\Users\TommySeery\source\repos\Concertable\b2b\.git\agent-workflow\runs\pr-disposition-20261002\review\8d41271e94c7536e41fb1abbe6c7376495ce418f3a645e09fcdd258439b8ac88`
**Candidate bundle identity:** `b0d86206e1986494d5616af9cd7b131117fa52a39150106d35efd4b19a0f59ec`
**Candidate patch identity:** `88fdfc32d1ec6b91eb28d8729bcf40575fdca01e6c2fa17ce80addb315f6d0b0`
**Candidate tree archive identity:** `01bb5fb67a8dbbb288e288d96a1ee3d3c910e170218213fb66f16f54d42f91c8`
**Work-order path:** `reviews/Feature-MonorepoBacklogPort.md`
**Work-order mode:** `append`
**Pass judgment:** `changes-requested`

Tracked-clean source synchronized once with merged main d8bec353 through native Git; untracked working review Markdown is preserved. Full owning-base candidate supersedes the broader erasure scope. Native Codex CLI and configured native-general role/model are unavailable; parent native fallback covers the exact frozen source with fresh independent general/module/test and security evidence contexts. Tier convention hashes remain cached from this logical workflow; no repository route table. Security explicitly covers the new admin PII export and unfiltered subject readers, despite the helper's generic classifier having no matching path. Read-only ownership, admin authorization, subject/tenant constraints, counterparty exclusion and absence of mutations are reviewed. Four unit and four PostgreSQL export cases passed; all 22 architecture tests passed after removing erasure-only test dependencies. Startup and exact-head remote CI remain required.


### Financial export findings

Both fresh immutable lenses completed and verified all supplied hashes. Parent native/source fallback confirmed the same 72-path candidate and retained two findings:

- [x] **F22 — MEDIUM — financial export regression coverage** — SubjectRightsApiTests:61–65 does not verify financial metadata values or existing-tenant isolation. Canonical seeds persist 14 contracts, but no invoices or self-billing agreements. Add owning-module PostgreSQL coverage with real invoice and agreement records, both invoice tenant sides, explicit expected contract cohorts and exclusion/empty-set behavior. No runtime defect was retained by the general lens.
- [x] **F23 — MEDIUM — counterparty tax identifier disclosure** — SubjectRecordMappers:12 exports InvoiceNumber verbatim. InvoiceIssuer:60 constructs it from supplier SellerIdentifier, which supports NI/UTR input. A customer export can therefore disclose another person's tax identifier. Omit the number from conservative invoice metadata and verify a customer export after minting an invoice for a supplier with a distinct tax identifier contains neither that identifier nor the encoded number.

Repairs are in progress against a later candidate: invoice number removed from DTO and mapper, nonempty contract assertion added, and Booking/Concert owner provider regressions added. The reviewed 9c9908c pass is complete with changes requested; final approval awaits passing repair regressions and immutable delta review.

F22/F23 repair evidence: Booking provider tests passed 3/3 using isolated valid contract factories and both tenant axes, combined deduplication, expected metadata and unrelated/empty sets. Concert provider tests passed 2/2 with a real settled invoice, both parties, agreement history and customer JSON excluding the supplier NI identifier and its encoded invoice number. The shared seed cardinality was not reliable; no package drift was proved and isolated owned records replace those expectations. Findings and original pass judgment remain unchanged; the repair must receive its own immutable incremental review before publication.
