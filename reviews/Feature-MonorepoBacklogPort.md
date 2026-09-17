# Code review — Feature/MonorepoBacklogPort

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `514894d6fc3190be4fab933183543a6a0705a2b7`  `(2026-09-17)`
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
  The export returns tenant-level records for every tenant the subject belongs to, including `SubjectContractDto.ArtistName` — a named individual when the counterparty is a sole trader. UK GDPR art. 15(4) says access must not adversely affect others' rights, and these are the tenant's records rather than the subject's personal data. No loaded doc rules either way; this needs an owner decision, not a silent default.

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

- [x] **F19 — LOW — docs** — `api/src/Modules/Concert/Concertable.B2B.Concert.Application/Interfaces/ISubjectRecordReader.cs:5`
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

- [ ] **F25 — HIGH — module boundaries** — `api/src/Modules/Conversations/Concertable.B2B.Conversations.Infrastructure/Services/SubjectMessageReader.cs:7`
  The extracted read-only reader is bound to `IMessagePrivilegedRepository`, the **writable** cross-tenant privileged context. `CODE_PATTERNS.md:29-31` allows `XPrivilegedRepository` "only where a cross-tenant write flow exists", and this reader has none — its two siblings created in the same commit use their module's read stance (`IBookingReadDbContext`, `IConcertReadDbContext`). Conversations has no read stance to use: only `ConversationsDbContext` and `ConversationsPrivilegedDbContext` exist, and `CODE_PATTERNS.md:15` omits Conversations from the read-context roster. Inherited from `ConversationsErasureService` rather than introduced, but the extraction was the moment to move the stance. Fix: add `IConversationsReadDbContext` (unfiltered read-only — the subject's messages genuinely cross tenants) plus an `IMessageReadRepository`, bind the reader to it, and add Conversations to the roster.

- [ ] **F26 — HIGH — test coverage** — `api/src/Modules/Conversations/Tests/`, `api/src/Modules/{Booking,Application,User,Venue}/Tests/*.UnitTests`, `api/src/Concertable.B2B.DataAccess/Tests/`
  Six test projects carry no `[assembly: AssemblyTrait("Category", …)]`, and `.github/workflows/ci.yml:73-74` filters on `Category=Unit|Integration|Architecture|Startup`. A test added to any of them compiles and is then skipped by the gate. Both Conversations projects are in that set, which is why the reader split could not be covered where it belongs. Fix: add an `AssemblyInfo.cs` to each, copying `Booking.IntegrationTests/AssemblyInfo.cs:3`.

- [ ] **F27 — MEDIUM — test coverage** — `api/src/Modules/Booking/Tests/Concertable.B2B.Booking.IntegrationTests/`
  Nothing has ever executed `IReadOnlySet<Guid>.Contains` inside an EF query against the real provider. `IReadOnlySet<T>.Contains` is a different expression-tree method from `ICollection<T>.Contains`; whether it emits `IN` is decided at runtime by SQL Server. The one in-tree precedent (`MessageRepository.cs:71`) ships but sits behind the same never-executed suite, and the only EF unit test in Conversations uses `UseInMemoryDatabase`, which evaluates client-side and would not expose a translation failure. Six queries share the construct, so they fail together or not at all. Fix: one real-provider call to `IBookingModule.GetSubjectContractsAsync(new HashSet<Guid> { … })` in `TenantScopingTests`.

- [ ] **F28 — MEDIUM — test coverage** — `api/src/Modules/Privacy/Tests/Concertable.B2B.Privacy.IntegrationTests/SubjectRightsApiTests.cs`
  `ToNullable()`'s `None` branch is asserted nowhere; if it yielded anything but `null` the export would emit a phantom user fragment unnoticed. One test exporting an unknown subject id asserts `JsonValueKind.Null` for `user` and simultaneously becomes the only coverage of every empty-set early return this pass added. Highest value per test in the set.

- [ ] **F29 — MEDIUM — correctness** — `api/src/Modules/Tenant/Concertable.B2B.Tenant.IntegrationTests/`
  `SeverMembershipsAsync`'s return is asserted nowhere, and no test subject belongs to two tenants, so the wound-down set — the value that drives `ScrubParticipantProfilesAsync` — is unexercised. `SubjectRightsApiTests` asserts only the severing side effect.

### Notes carried, not raised as findings

- `Concert.Contracts` → `Deal.Contracts` **is not** a boundary violation: `Directory.Build.targets:41-48` flags only paths escaping the service root, `Booking.Contracts` already carries the identical reference, `Deal.Contracts` is `IsPackable` and listed in `.github/b2b-promotion-candidates.json`, and `MODULES.md:20` permits Contracts→Contracts for shared base types. Conclusive.
- `ToNullable` is Reunion-provided (`Reunion.OptionReferenceNullableExtensions`), so `CARRIERS.md:238`'s ban on *local* nullable helpers does not apply.
- `StartupTests.ResourceGraphTests.ProductionGraphAndStrictValidation_AreValid` fails locally with a `TimeoutException` inside `Concertable.Payment.Hosting.AppHostExtensions.AddStripeCli`. No path in this range touches AppHost/hosting/Aspire and the suite predates the branch, so it is not attributed to this candidate — but it is unverified, not green.
- Removing the `SettledStates` comments was requested and is compliant with the zero-comment default; the invariant they stated belongs in the test F18 already asks for.
