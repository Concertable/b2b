# Code review — Feature/MonorepoBacklogPort

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `c4e1e88e3d2c181410c4b695e09deb141465f8aa`  `(2026-09-15)`
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

- [ ] **F1 — CRITICAL — correctness** — `api/src/Modules/Booking/Concertable.B2B.Booking.Infrastructure/Services/ObligationChecker.cs:13`, `api/src/Modules/Application/Concertable.B2B.Application.Infrastructure/Services/ObligationChecker.cs:13`
  Erasure can never complete for any subject with booking history. `SettledStates` omits `BookingState.Confirmed` and `ApplicationState.Accepted`, but `BookingStateMachine.cs` gives `Confirmed` no outgoing edge and `ApplicationStateMachine.cs` gives `Accepted` none — both are terminal, so both count as a live obligation forever. A booking confirmed in 2024 whose concert completed and invoiced still defers the DSAR, and the hourly sweep re-defers it every hour indefinitely. Two of the three gates can never open. Fix: count only genuinely in-flight states, and let the Concert checker — which has a real terminal `Complete` — own post-confirmation settlement.

- [ ] **F2 — CRITICAL — security** — `api/src/Modules/Conversations/Concertable.B2B.Conversations.Domain/Entities/MessageEntity.cs:62`
  The export re-assembles every erased subject's messages. `SeverAuthor()` sets `SentByUserId = Guid.Empty`, and `MessagePrivilegedRepository.cs:16-17` matches that column by equality over the unfiltered privileged context. `GET /api/subject-export/00000000-0000-0000-0000-000000000000` satisfies the `{subjectId:guid}` constraint and returns the retained message bodies of every previously-erased subject across every tenant, in one download — defeating the sever it exists to complete. Fix: make `SentByUserId` nullable and sever to `null`, or reject the empty GUID at both service entry points.

- [ ] **F3 — CRITICAL — correctness** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectErasureService.cs:93`
  A re-driven erasure permanently skips the profile scrub and the invitation purge. The wound-down tenant list is the return of `SeverMembershipsAsync` and the email is read from the user row before tombstoning. On a second pass the memberships are already gone, so the return is empty and `ConversationsErasureService.cs:33-34` short-circuits; the email is already tombstoned, so no invitation can match. Making erasure re-drivable without making the fan-out idempotent *as composed* means a resumed run silently leaves `ParticipantProfile` rows — a sole trader's name and address — and pending invitations intact, with no signal. Fix: capture the wound-down tenant set and the subject's email onto the request row on the first pass and drive later passes from that durable state.

- [ ] **F4 — HIGH — correctness** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Repositories/SubjectErasureRepository.cs:12`
  A crash mid fan-out strands the subject in `InProgress`, invisible to the sweep. `DriveAsync` commits `InProgress` before the non-atomic five-call fan-out, and `ListDeferredAsync` lists only `Deferred`, so the `(InProgress, Begin)` edge added for exactly this case has no caller. `ErasureTrigger.Fail`, `RecordFailure` and `ErasureState.Failed` have no production caller anywhere, and `Failed` has no outgoing edge, so it is a trap state the moment it becomes reachable. Fix: widen the work list to include `InProgress`; wrap the fan-out so a hard error fires `Fail` plus `RecordFailure` and persists; add a `Failed` recovery edge.

- [ ] **F5 — HIGH — module boundaries** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/DeferredErasureRunner.cs:23`
  The sweep runs every subject through one shared DI scope, so a subject whose `SaveChangesAsync` throws leaves dirty tracked entities that the next subject's save flushes — the per-item try/catch gives isolation it does not provide. Breaks `api/src/Modules/Concert/AGENTS.md:76` ("background completion invokes the workflow directly through a fresh scope"), whose precedent is `CompletionRunner.cs:27-33` collecting ids and using `IScoped<T>`. Fix: list subject ids, inject `IScoped<ISubjectErasureService>`, re-read inside the scope.

- [ ] **F6 — HIGH — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectErasureService.cs:103`
  A typed conflict is thrown away as an exception: `Advance` builds `ErasureTransitionError` then throws `InvalidOperationException` carrying only its message, discarding a `Conflict` definition. `(InProgress, Defer)` is missing from the table, so an operator re-POST after a crashed run returns an opaque 500 rather than 409. The house convention is a returned union case (`ApplicationService.cs:221-222`, `SettlementService.cs:152-153`), and `SubjectRightsController` is the only controller of 21 with no Result terminal — `SelfBillingAgreementController.cs:36-41` is the file-returning precedent. Fix: return a Result, terminate it in the controller, add `Reunion.AspNetCore` to `Privacy.Api.csproj` (the arch guard requires direct ownership), add the missing edge.

- [ ] **F7 — MEDIUM — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Api/Controllers/SubjectRightsController.cs:14`
  `[Authorize(Policy = "Admin")]` duplicates the literal `AdminAttribute` owns. `ModerationController.cs:5,18` is the cross-module precedent for taking the `Admin.Api` reference and using `[Admin]`. A policy rename would not fail to compile here; it would 500 at request time.

- [ ] **F8 — MEDIUM — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Api/Controllers/SubjectRightsController.cs:16`
  The only controller in the tree with no class-level `[Route]`; all 20 others have one. A later action added without an absolute template would have no route at all.

- [ ] **F9 — MEDIUM — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Domain/Lifecycle/ErasureStateMachine.cs:7`
  Hand-rolls what the kernel `StateMachine` provides and is DI-registered, where `BookingStateMachine`, `ApplicationStateMachine` and `ConcertStateMachine` are one-line derivations held static on their entity. `SubjectErasureRequestEntity.Transition` is a state setter with no guard, so the aggregate's state can be set without consulting its own machine.

- [ ] **F10 — MEDIUM — docs** — `CODE_PATTERNS.md`, `ARCHITECTURE.md`
  Neither is updated. Privacy is a fifth untenanted DbContext absent from the stance roster, and `ConversationsPrivilegedDbContext` is now used for GDPR erasure while its own doc and the roster still say moderation only. Root `AGENTS.md` names `CODE_PATTERNS.md` as *the* stance roster, so a stale roster is the mechanism by which the next change picks the wrong stance.

- [ ] **F11 — MEDIUM — module boundaries** — `api/tests/Concertable.B2B.IntegrationTests.Fixtures/PrivacyApiFixture.cs:3`
  Sits in the shared cross-module fixtures project; all 13 siblings live in their own module suite. Every module's integration suite now compiles against a Privacy-specific type.

- [ ] **F12 — MEDIUM — module boundaries** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Application/DTOs/SubjectErasureRequestDto.cs:5`
  `public` where sibling Application DTOs are `internal`, and `FileDownload.cs:3` beside it is internal. The `InternalsVisibleTo` chain already covers its only consumer. Privacy has no Contracts project, so this is the whole public surface of an Application assembly that Workers also references.

- [ ] **F13 — MEDIUM — product decision** — `api/src/Modules/Booking/Concertable.B2B.Booking.Contracts/IBookingModule.cs:29`
  The export returns tenant-level records for every tenant the subject belongs to, including `ContractExport.ArtistName` — a named individual when the counterparty is a sole trader. UK GDPR art. 15(4) says access must not adversely affect others' rights, and these are the tenant's records rather than the subject's personal data. No loaded doc rules either way; this needs an owner decision, not a silent default.

- [x] **F14 — MEDIUM — efficiency** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectObligationChecker.cs:29`
  `GetLiveObligationCountAsync` returns a count no caller reads as a number — it is only compared against zero. Three counting scans per check where an existence check short-circuits on the first row.

- [ ] **F15 — MEDIUM — efficiency** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Repositories/SubjectErasureRepository.cs:12`
  The sweep's work list is unbounded: no cap, processed serially, four queries per subject. With F1 in place deferred rows only accumulate, so each hourly pass costs strictly more than the last.

- [ ] **F16 — MEDIUM — conventions** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Domain/Lifecycle/ErasureTransitionError.cs:15`
  Error code uses `invalid_transition` where all seven existing transition errors use the `invalid_state` segment.

- [ ] **F17 — MEDIUM — test coverage** — `api/src/Modules/Privacy/Tests/Concertable.B2B.Privacy.UnitTests/ErasureTransitionErrorTests.cs:14`
  Asserts `Contains` on the message and never asserts the error kind, so the 409 could silently become a 422. `ErrorDefinitionContractTests.cs:180-193` pins code, exact message and kind for all 24 Concert cases.

- [ ] **F18 — MEDIUM — test coverage** — `api/src/Modules/Privacy/Tests/`
  Ranked gaps: no `DeferredErasureRunnerTests` at all; nothing anywhere exercises a real `SettledStates` list, which is exactly why F1 shipped with a green suite; no `SubjectExporterTests`; `ScrubParticipantProfilesAsync` and `PurgePendingInvitationsAsync` are asserted nowhere in the tree; `SubjectRightsApiTests` has never executed and no seeded subject has exactly one class of obligation, so it cannot isolate a regressed checker.

- [ ] **F19 — LOW — docs** — `api/src/Modules/Concert/Concertable.B2B.Concert.Application/Interfaces/IConcertExportReader.cs:5`
  Doc claims it returns contracts; those come from Booking.

- [ ] **F20 — LOW — correctness** — `api/src/Modules/Privacy/Concertable.B2B.Privacy.Infrastructure/Services/SubjectErasureService.cs:109`
  Discards the `TryGetValue` bool, so a union extension would silently yield `default` = `Requested`, rewinding a live request. Masked today only by the throw that F6 removes.

- [ ] **F21 — LOW — conventions** — `api/src/Modules/Application/Concertable.B2B.Application.Contracts/IApplicationModule.cs:21`
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
