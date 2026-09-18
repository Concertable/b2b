# B2B authorization model

**The current authorization model will not carry the product through delegated signing and configurable, party-bound work. Replace the single fixed role per membership and scattered resource decisions with composable tenant RBAC plus typed relationship authorization, evaluated in process against module-owned relational tables.** Keep the active-tenant boundary, membership incarnations and finite resource scopes. Add one resource authorization API, shared query predicates and command transaction fences. Do not introduce an external authorization service or a general policy language. This is the selected model for the next two years, including the role-customization work; it supersedes the unowned `Authorization/ComposableRoles` deferral.

This is an implementation decision, not a claim of delivered behavior or an instruction to change production code in this research commit. The delivery dependencies below belong to the existing foundation work. This reference creates no separate phase ledger. Research date: **18 September 2026**.

## 1. Evidence and corrections

### Inspected baseline

Source citations below pin B2B to `e5263bb44f7b81fb145869ba85d3ff92372b0c8e`, the local HEAD containing the review. The runtime candidate reviewed was `735eae64af97d8431ed93b2270234ac39bfb4b94`. Existing working-tree changes were excluded from this decision's commit. Central product documents were fetched through authenticated GitHub reads at docs commit `466910f82dabf1b20ec1cf6e20a89f3351813d74`; the public web fetch could not read them.

The inspection enumerated and read all **28 files** under `api/src/Modules/Authorization`, including its tests and project files; the shared grant, authority, configuration and context types under DataAccess; all six concrete grant entities, scope enums and configurations; all four resource-scoped contexts; Concert's ACL services, domain methods and both repository stances; Tenant membership administration; and the shared client's permission contract. Searches also covered `api/src`, `app`, `plans`, the root solution/package manifests and CI configuration for permission consumers, grant issuers, authorization handlers, policy engines and transaction/check abstractions. Generated migrations/build outputs were excluded from behavioral searches. Negative findings here apply to that enumerated repository surface, not to uninspected sibling services or the live product.

| Present mechanism | Verified behavior and source |
|---|---|
| Request authority | `MembershipContext.cs:28–71` resolves authenticated user plus selected `X-Tenant-Id` to a current membership. A sole membership can supply an omitted header; ambiguous membership denies, and malformed explicit headers throw. `IsHost` is false. Permission handling reads this context, not role claims. [Resolution][C1], [handler][C2] |
| Member capabilities | Six `TenantRole` values; exactly one role on `TenantMembershipEntity`. Twenty-two permission constants. `PermissionCatalog` explicitly repeats grants in six frozen sets. Audience is requested per permission, but its width is still selected from the role: Owner/Manager/Finance get tenant reach, Staff/Door/Sound assigned reach. [Roles][C3], [membership][C4], [permissions][C5], [catalog][C6] |
| Resource disclosure | `ResourceAccessGrant<TScope>` records resource ID, recipient tenant, optional membership incarnation, scope, validity, revocation, issuer, kind and version. The concrete families retain typed resource ownership. This is a direct relationship/ACL model already; it is not pure RBAC. [Grant][C7], [kinds][C8] |
| Live membership | `ResourceAccessExpressions.cs:8–21` checks one authority row matching MembershipId, TenantId, UserId and PermissionVersion, then recipient tenant and validity. `MembershipAuthority` is a keyless read mapping to a Tenant-owned view. [Expression][C9], [view mapping][C10] |
| Reads | Global EF filters compose that common half with module-specific audience/scope expressions. Parents reach rows through `Any` over grant sets. There is no resource-aware central Check/List contract. [Concert][C11], [Booking][C12], [Application][C13], [Conversations][C14] |
| Commands | There are real authorization functions outside filters: `HasPermission`, Concert's `IsPrincipal` and grant mutation guards, and `ThreadEntity.Admits`. Privileged ACL repository methods exist. What is missing is a consistently consumed, resource-aware decision/fence boundary. [Concert domain][C15], [privileged repository][C16], [thread][C17] |
| Storage | Current B2B uses EF Core's SQL Server provider. Grant indexes include resource-first and tenant-first lookups and separate active uniqueness rules for nullable/non-null membership targets. PostgreSQL in Deal's target design is future architecture, not the provider this branch runs. [Packages][C18], [grant configuration][C19] |
| Authoring drift | The client manually declares **19** permission literals against **22** backend constants: `terms.read`, `bookings.cancel` and `concerts.declare_door_revenue` are missing. The catalog tests check declared/granted set coverage, not whether any valid resource path can exercise each grant. [Client][C20], [catalog tests][C21] |

The actual scope roster is:

| Grant family | Enum values at this HEAD | Filter owner |
|---|---|---|
| ApplicationAccessGrant | Summary, Proposal | ApplicationDbContext |
| BookingAccessGrant | Summary, Operations | BookingDbContext |
| ContractAccessGrant | Read | BookingDbContext |
| ConcertAccessGrant | Summary, Operations, Finance | ConcertDbContext |
| InvoiceAccessGrant | Read | ConcertDbContext |
| ThreadAccessGrant | Read, SendMessages | ConversationsDbContext |

These values are established by the six scope declarations and the filters cited above. `terms.read` is a permission; Contract's enum currently says `Read`, not `Terms`. The union of scope vocabularies is finite per resource, not one global hierarchy. Summary never implies Finance, a contract download or message sending.

The grant's tuple shape resembles Zanzibar's object/relation/subject data. Enumeration found no authorization schema language, userset rewrites, arbitrary/transitive relation traversal, central resource Check API or graph consistency token implementation in this tree. A `Version`/`PermissionVersion` concurrency field is not a Zanzibar consistency token. Zanzibar supplies a policy configuration model and causal consistency machinery as well as tuple storage. [Original Zanzibar paper][Z1]

### What the review proves

The requested findings are in the **18 September full P1 review**, beginning at review line 295; that is the fourth review heading in the file. They support the following judgments, rather than proving that any particular library is necessary. [Review][R1]

| Finding | Verification and judgment |
|---|---|
| R1: ACL commands see only the caller's grants | Confirmed in five load paths: share, replay, revoke, assign and remove assignment (`ConcertService.cs:320,368,387,415,440`). The normal repository includes grants through the recipient-filtered context. Other recipients and expired grants disappear before the domain checks run. **Immediate implementation defect, exposing an architectural weakness.** The brief overstates the absence of a mechanism: the privileged repository already has complete-ACL and identity methods, and plan §4.5 explicitly specifies the correct path. Wire and fence that path; do not rewrite storage to fix this defect. [Service][C22], [normal repository][C23], [privileged repository][C16], [specified command path][P1] |
| R17: repeated audience logic | Confirmed duplication, but the audited source has **nine** repeated audience branches: Concert 3, Booking 2, Application 2, Conversations 2. The review says ten; its listed sites also total nine. **Implementation duplication and a missing reusable enforcement primitive**, not evidence that a graph engine is required. The shared expression stops before audience evaluation. [Concert][C11], [Booking][C12], [Application][C13], [Conversations][C14], [common half][C9] |
| R22: Staff messaging cannot be exercised | Confirmed for production grant issuance. Staff receives `messages.read/send` with AssignedResources, while thread creation issues only tenant-wide grants. The only production issuer of `MemberAssignment` is `ConcertEntity.AssignMember`, for Concert Summary/Operations. The generic Thread grant factory accepts a membership ID, but no production path supplies one. **A composition defect in the model's current realization:** independent role and grant catalogs can promise an unreachable capability. Add reachability qualification and a deliberate conversation-assignment path. [Catalog][C6], [thread issuance][C17], [Concert assignment][C15] |

R1 can be repaired without changing the authorization model. R17 and R22 can also be repaired locally. The reason to change the model is the combination of ongoing authoring drift, required customer-controlled bundles and the product relationships below. Conversely, none of these findings justifies leaving the existing critical defects until the redesign finishes. The same review also contains the settlement-trigger and publication criticals and the excluded, stale Authorization test project; this research neither closes them nor reports new runtime test results. [Review][R1]

The brief's quoted sentence about not inflating P1 is not verbatim at this HEAD. The actual §4.1 deferral is at lines 337–357: a provisional `Authorization/ComposableRoles` plan owns multiple assignments and custom bundles. The only matching plan reference in the enumerated local plan corpus is that paragraph. This decision supplies its missing design and binding delivery position. [Existing deferral][P2]

### Product requirements that determine the model

1. **Configuration composes implemented steps inside fixed stages.** It does not introduce arbitrary lifecycle stages, executable customer policies or new semantic operations by inserting a row. The two routes remain Opportunity → Application → Booking → Concert and Direct Invitation → Booking → Concert. A customer-defined rider review can reuse `requirements.decide`; it does not need `approve_template_123`. A genuinely new act still needs a deployed permission, handler, typed inputs and policy. [Workflow capabilities][D1], [fixed routes][D5], [Deal target §5.3–5.5][D2]
2. **The party axes are independent.** Show participation, agreement principal, acting representative, respondent, approver, payer, beneficiary and data audience cannot be collapsed into an Owner/Manager/Staff table. A production reviewer must see evidence without fees; an agency can sign without collecting money; two artists in one show keep private agreements. The product authority already describes bounded resource relations, separate authority grants and no initial transitive delegation. [Expressiveness, party design and four authorities][D3]
3. **Authority is live while agreed content is pinned.** An approval targets one evidence/assignment revision. A signature records both actor and represented principal. Revocation changes future access and unaccepted consent qualification; it does not rewrite a valid accepted signature or completed money. [Foundation signing][P3], [evidence approval][P4], [expressiveness][D3]
4. **Tenant-owned configurations need author/publish/use boundaries.** Capability eligibility and product entitlements remain separate from member permission and resource visibility. Losing entitlement to author a new configuration must not erase authority to fulfill/refund an already accepted obligation. [Deal target §5.5][D2]
5. **Growth concerns are real without being launch requirements.** The nightclub benchmark separates announcement approval, production approval, funding and final settlement, and reopens review for a revised rider without silently changing the fee. It is hypothetical product input, not evidence of Fabric's private arrangements or a demand to ship collection mandates now. [Nightclub benchmark][D4]

There are **five named structures** in the brief: ShowParticipant, AgreementParticipant, RevisionParticipant, ParticipantBinding and ParticipantSlotDefinition. The last defines a slot's schema/cardinality; it is not itself an authorization edge. Reading §§5–9 yields the following **13 authority-relevant relation families**, counting each row once rather than counting every slot kind or scope as another relationship:

| Relationship | Owner / introduced with | Authorization implication |
|---|---|---|
| Show ↔ participating tenant | Show / P2 | Descriptive participation; no automatic child agreement access |
| Agreement ↔ stable tenant participant | Entry / P2 | Tenant identity cannot be retargeted |
| Revision ↔ participant/capacity | Entry, accepted copy in Booking / P2 | Authority refers to the exact agreed principal set |
| Revision + slot ↔ participant binding | Deal schema, stage-owned values / P2 and P5 | Payer, Payee, Respondent and Approver are separate bindings |
| Show ↔ engagement | Show grouping projection / P2 | Grouping is not permission inheritance |
| Venue operator ↔ authorized slot/location/schedule | Show's VenueUseAuthorization / P2 | Physical use authority; not commercial disclosure |
| Consent ↔ revision/principal/acting member | Entry / P2 | Validate every required principal's qualifying consent |
| Representative participant ↔ principal participant | Entry/Booking / P3 | Distinguish actor from represented legal party |
| Principal tenant ↔ acting tenant, scoped act | Tenant AuthorityGrant / P3 | Explicit signing mandate; no transitive chain |
| Derived resource grant ↔ exact authority version | Resource owner / P3 | Source revocation must synchronously invalidate access |
| Direct invitation ↔ addressed principals/proposer | DirectInvitation / P4 | Sending is not recipient consent |
| Requirement ↔ approver participant/member assignment | Concert / P5 | Named reviewer, replacement and revocation |
| Decision ↔ assignment version and evidence revision | Concert / P5 | A stale decision cannot authorize the current consequence |

This inventory comes from the [schema/attachment contract][P5], [consumption contracts][P6], [signing][P3] and [requirement specification][P4]. Several rows are domain facts or historical evidence rather than grants. Their count is not an argument for arbitrary graph traversal. Their **different meanings** are the argument against using a six-role table as the whole decision.

### Muzeek: established behavior and limits

Public first-party documentation supports more than a fixed role list:

| Public evidence | What Concertable needs to match the evidenced behavior |
|---|---|
| The team-member guide, dated 11 October 2021, describes Administrator and Standard users, with selectable visible sections for Standard users. [Guide][M1] | Configurable member capability bundles and a usable permissions editor |
| The ownership guide distinguishes an Owner with billing/admin access and exclusive account-deletion authority. [Guide][M2] | Protected ownership and last-owner administration, separate from business-resource authority |
| The roster guide describes agencies, management companies and venue groups working across rostered accounts, with controls over message flow, deal terms, booking actions and paperwork. [Guide][M3] | Explicit cross-business representation and independently scoped disclosure |
| Restricted roster access defaults to the collective's own created bookings/events plus limited calendar information; full access can include other creators' records. A business can join multiple rosters. [Guide][M4] | A bounded relationship to the represented tenant, resource provenance and explicit portfolio/disclosure policy; joining a roster cannot mean membership in every tenant |
| Current Muzeek API docs expose event read/write scopes and a wildcard API-key scope. Those are integration capabilities, not proof of its human-role implementation. [Developer docs][M5] | Later integration credentials need their own scoped identity design; do not copy API-key semantics into human membership |

The current [Muzeek site][M6] advertises custom permissions and rosters, and the [2026 product pricing page][M7] still lists roster/collaboration configuration. The detailed help articles are older. They establish publicly described behavior, not independently tested current enforcement. The inspected public sources do **not** establish an internal Zanzibar deployment, role inheritance, arbitrary custom role definitions, a complete action-level matrix, revocation consistency or cryptographic signing-mandate verification. The benchmark therefore supports configurable permissions and relationships; it does not select a backend protocol.

To match restricted/full roster behavior later, use Tenant-owned, one-hop representation plus explicit resource-owner disclosure policies. Restricted mode can require immutable `CreatedForTenantId`/`CreatedByActingTenantId` provenance under that relationship; full mode explicitly admits a bounded portfolio of supported resource kinds/scopes. The represented tenant approves the relationship. Private counterparty disclosure commitments still apply. The acting membership stays in the agency tenant. Collection and signature authority remain separate permissions/mandates even if the product offers one combined setup screen. This is a concrete extension of the selected model, not a requirement to ship broad roster delegation in P1 or P3.

## 2. Options and decision

| Option | Fit for this product | Decision |
|---|---|---|
| Six fixed roles + local repairs | Can make current sharing safe. Cannot give customers different bundles or cleanly compose per-operation audience widths. New relationships continue accumulating bespoke command checks. | Necessary repairs, insufficient target |
| Central Check API over existing tuples, unchanged role storage | Separates ACL administration from recipient reads, unifies decisions and supports SQL lists. Does not itself add custom roles, mandate versions or assignment/revision facts. | Adopt as the first delivery boundary, then complete the selected model |
| OpenFGA | Has typed relationship models, conditions, contextual organization authorization and Check/List operations. Would require membership-incarnation subjects, explicit active-tenant intersection, per-facet relations and time conditions. | Reject for this two-year design |
| SpiceDB | Has typed relations, computed permissions, caveats, expiring relationships and consistency controls. Strong candidate for a genuinely shared, distributed authorization graph. | Reject for this two-year design |
| Self-hosted generic Zanzibar equivalent | Retains the same graph/schema, list-query and cross-store transaction integration work; building one also makes Concertable responsible for the engine. | Reject; do not build a home-grown graph server |
| Cerbos | Resource policies, principal attributes and CheckResources/PlanResources can express the checks. The application still supplies authoritative relations and translates/enforces data filters. | Reject as the primary engine here |
| Casbin | Embedded enforcement and tenant/domain roles are viable. Matching policies still need incarnation, validity, resource facts, database filtering and transaction integration. | Reject; a generic matcher adds little to these typed EF-backed decisions |
| OPA | Rego can express the rules and partial evaluation can produce data filters. A policy/data distribution model, supported filtering fragment and database integration still need qualification. | Reject; broader policy authoring is not an evidenced customer requirement |
| Composable RBAC + typed relationships + in-process Check/query/fence | Makes member customization data-driven; retains exact domain ownership and one database transaction for live authority; handles bounded joins and finite configuration. | **Selected** |

OpenFGA's [conditions][F1] and [organization-context model][F2] demonstrate that time and active-tenant context can be modeled; they are not automatic consequences of adopting it. Its [higher-consistency mode][F3] bypasses caches. SpiceDB offers [caveats][S1], [relationship expiration][S2] and [ZedToken-based consistency][S3]. A token ensuring a sufficiently fresh graph read does not hold a B2B SQL membership, mandate or accepted revision stable until a later SQL commit. A fully consistent check also does not see changes made after that check starts.

**The reason to reject a generic graph engine is specific to this product.** The demanding checks depend on exact revision/hash, immutable principal bindings, current reviewer assignment, current mandate version, provenance, scope and lifecycle consequences. The product explicitly excludes transitive delegation and arbitrary tenant-authored semantics. Shared Show ancestry must *not* confer agreement visibility. Its natural graph-like joins are short, typed and owned by the same B2B database. Moving those facts to a second authoritative store would not remove the transaction and disclosure work; it would separate it from the domain records it must fence. ReBAC as a modeling technique is selected. A universal relationship engine is not.

For OpenFGA/SpiceDB adoption, a workable design would still need synchronous revocation barriers in SQL, ordered graph writes, fail-closed handling while graph publication is incomplete, policy/model-version coordination, and a list strategy that respects business ordering/counts without fetching all permitted IDs. Sharing a physical database server does not enlist a remote service's transaction in the application's transaction. An asynchronous outbox alone cannot meet the stated revocation requirement. Those integration costs are not solved by a tuple resembling `object#relation@subject`.

Operationally, either external engine adds a deployable and datastore lifecycle, service credentials, availability/backup/restore coordination, tracing and failure handling. Every uncached remote check adds a network round trip; bulk checks reduce trips, not the consistency problem. On unavailable authority, private reads/writes fail closed. No latency or infrastructure price is asserted here. The selected design pays for additional SQL predicates/indexes and lock contention instead; that cost must be measured too.

Cerbos is the strongest policy-engine alternative because [PlanResources][E1] expressly supports filtering before pagination. OPA also has [SQL data filtering][E2], subject to its [supported policy fragment][E3]; it would be wrong to dismiss either as requiring per-row remote checks. [Casbin domain RBAC][E4] supports tenant-specific roles and can run embedded. Nevertheless, none supplies B2B's live authority facts or commit fence. For this C#/EF modular service, exporting those facts into a second policy representation is more integration surface than composing the same typed SQL predicates used by queries. This is an engineering judgment, not a claim that these products cannot express the rules.

### Apply the same rejection bar to every option

| Required property | Selected model | Graph engine would need | Policy engine would need |
|---|---|---|---|
| Active tenant | Resolve and validate membership once per request; revalidate identity/version in SQL | Server-supplied tenant context intersected with the queried incarnation; never union all user tenants | Server-supplied principal/tenant attributes and matching rules |
| Membership incarnation | Every member grant/assignment names MembershipId; exact current membership join | A new subject identity on rejoin, plus membership-edge validity | MembershipId as subject, authoritative current-membership input |
| Audience width | Maximum permitted width **for the requested operation**, followed by recipient/member restriction | Distinct tenant-wide/member-specific branches gated by that operation | Explicit matching conditions; tenant/domain role alone is insufficient |
| Validity/revocation | Live interval, revoked state and source-version joins; transaction fence | Conditions/expiration plus synchronous relationship updates and SQL commit coordination | Trusted current facts/time; cache invalidation is not a commit fence |
| Finite disclosure | Resource-owned typed scopes and approved projections | Separate resource/facet relations; a broad parent relation must not imply terms | Explicit scope attributes and projection filtering |
| In-flight write | Locks and final recheck in the same business transaction | B2B fence still required after/around Check | B2B fence still required after/around evaluation |
| Lists/downloads | SQL EXISTS before count/order/page; scope-specific download checks | Query/list integration and current authorization on content delivery | Qualified filter translator and current authorization on content delivery |

This is the same standard as the foundation's [Finbuckle decision][P7]. Finbuckle's single-owner isolation would not replace shared-resource policy. No new library is admitted merely for having an RBAC or multi-tenant label.

## 3. Implementation design

### 3.1 Decision formula and ownership

For a private operation, require:

```text
current request membership incarnation and versions
AND effective permission for this operation
AND resource-specific audience/scope access, where the operation requires it
AND this operation's principal/assignment/representation relationship
AND current validity and required subject revision
```

The owning domain operation then enforces lifecycle, accepted terms and business preconditions. For example, `requirements.decide` plus an assignment can authorize a reviewer to attempt a decision, while stale evidence or an invalid transition prevents that decision taking effect. A read grant never proves authority to approve, sign, share or move money. There is no blanket Owner bypass.

| Location | Responsibility |
|---|---|
| Authorization.Contracts | Permission/descriptor contracts, membership snapshot and repository port, resource address/request/decision contracts, `IResourceAuthorization`, request attributes |
| Authorization.Infrastructure | Request resolution, permission catalog validation, registry dispatch and ASP.NET policy adapter; no direct dependency on other modules' entities or infrastructure |
| Tenant Domain/Application/Infrastructure | Memberships, role definitions/assignments, ownership administration, authority versions and representation mandates; authoritative reads/fences through narrow contracts |
| DataAccess.Application/Infrastructure | Shared grant primitives, expression composition, mapped authority views, read-filter registration and command transaction/fence mechanics; no Concert/Show/Deal business rules |
| Each resource module | Its permission manifest, typed policy fact queries, resource/grant/assignment schema, permitted disclosure and mutation policy; registers its evaluator at composition |
| Deal | Finite capability/slot declarations and compatibility checks; never makes a runtime permission decision on behalf of a stage |
| Application/DirectInvitation/Booking/Concert | Revision bindings, consent/acceptance, requirements and enforcement of their own lifecycle effects |
| Shared client | Generated permission identifiers; server-provided role metadata/effective permissions and resource actions. Role names never determine access locally |

Authorization dispatches through its own neutral evaluator port, implemented by the owning module. DataAccess may depend on Authorization.Contracts; Authorization.Infrastructure must not depend back on DataAccess or on resource modules. The composition root binds evaluators. No universal table with unchecked polymorphic resource IDs replaces the six FK-backed families.

### 3.2 Composable tenant roles

Replace `TenantMembershipEntity.Role` with many role assignments. Introduce these Tenant-owned relational records:

| Record | Required fields / constraints |
|---|---|
| TenantRoleDefinition | Id, TenantId, Name, Version, optional SystemPresetKey, IsProtectedOwner, retired state; unique tenant/name; immutable tenant identity |
| TenantRolePermission | TenantId, RoleId, PermissionKey, Audience; unique role/permission; composite FK to role's tenant; unknown permission/audience rejected |
| MembershipRoleAssignment | TenantId, MembershipId, RoleId, issuer membership, created time; unique membership/role; composite FKs prevent assigning a foreign-tenant role |
| Tenant.RolePolicyVersion | Monotonic bigint incremented for role-definition/permission changes affecting effective authority; separate from business eligibility versions |
| Membership.PermissionVersion | Incremented for assignment changes; membership removal destroys current authority and rejoin allocates a new MembershipId |
| Tenant-owned AuthorizationCatalogState | Singleton installed manifest revision/hash, changed atomically with generated system-preset updates; used by startup/readiness validation |

Owner is a protected system role. Manager, Finance, Staff, Door and Sound remain useful **system presets**, not the universe of roles. System presets are immutable through customer APIs; customers clone a preset or create a named custom role and edit its known permissions/audiences. Seed system role rows per tenant from the generated manifest. Updating a deployed preset is an explicit catalog update transaction that updates its rows and increments affected tenants' RolePolicyVersion. Custom roles never acquire a newly introduced permission automatically. Unknown permission names always deny; Owner's grants are explicitly declared, not a wildcard accepting future strings.

Effective grants are the union of all assigned roles, grouped by permission. For each permission choose the maximum width: None < AssignedResources < TenantResources. There is **no inheritance and no explicit deny precedence**. An absent permission in one role does not cancel a grant in another. The role editor shows the combined result, including when another role widens it. This provides the requested composition without an unevidenced hierarchy language.

TenantResources means tenant-wide grants **or grants to this same membership**. It does not let a manager read a different member's restricted grant. AssignedResources requires the exact current MembershipId; a tenant-wide grant does not satisfy it. A direct member grant can be sufficient without a redundant tenant-wide disclosure row.

Only a protected Owner can create/edit roles, assign arbitrary roles, grant/remove Owner or retire roles. Catalog metadata marks ownership/role-administration operations `OwnerOnly`; custom-role writes reject those permissions rather than advertising an unusable administrative capability. Removing a role in use requires a replacement assignment in the same transaction. Last-owner checks and owner changes lock the tenant row. A holder of `members.invite` can invite only explicitly invitation-assignable, non-administrative roles whose effective permission/audience set is a subset of the inviter's authority; the initial allowed presets remain Staff/Door/Sound. Validate again when accepting the invitation. Role administration never bypasses resource ownership, accepted disclosure policy or another tenant's mandate.

Return role IDs/names, effective `(PermissionKey, Audience)` pairs and membership/policy versions through membership APIs. Change invitation, role editor, web/mobile membership contracts, seeds and clients together. Delete the single-role enum-dependent authority path on replacement; do not retain a `PrimaryRole` compatibility property.

The new resolved membership carries MembershipId, TenantId, UserId, PermissionVersion and RolePolicyVersion plus its computed permission set. Extend the keyless `MembershipAuthority` view to join the current tenant policy version. Each SQL decision compares both versions and the exact incarnation. A changed role definition invalidates an in-flight snapshot without incrementing every membership row. The installed catalog revision must match the server's generated manifest before the server becomes ready; a partially applied preset update cannot serve requests under mixed catalog semantics. Request-local memoization is allowed; cached authorization across requests is not introduced. Catalog generation/version is included in cache keys if caching is later justified.

### 3.3 Resource relationships and storage

Keep module-owned `ResourceAccessGrant<TScope>` families and the existing issuer/kind/validity fields. A direct grant is an authoritative disclosure record. Add only real, consumed relationship data:

- P2 stores the Show/participant/revision/binding relationships specified by the foundation. A participant label does not synthesize an ACL. Accepted principals receive the explicitly agreed scopes through their resource owner's creation command.
- P3 adds Tenant's AuthorityGrant and immutable AuthorityGrantVersion, with principal/acting tenant, supported act, agreement-or-show bounds, constraints, evidence and validity. A derived resource grant records SourceAuthorityGrantId and SourceAuthorityVersion. Each read/check joins the live Tenant-owned status view and requires the exact version to remain current, in scope, unrevoked and in time. Replacing a mandate does not revive old derived rows. No transitive delegation is admitted.
- P5 adds ApprovalAssignment under the requirement owner. It identifies RequirementId, approver participant, optional exact reviewer MembershipId, version, validity and revocation. A named member must currently belong to the bound approver tenant. ApprovalDecision records the exact assignment/evidence/subject revision and actor. Assignment does not grant missing evidence access; its creation validates that access through the evidence owner's policy.

Retain module-local resource FKs, immutable recipient/issuer identity and grant version concurrency checks. Preserve the two active uniqueness indexes for nullable/non-null membership targets; expired unrevoked grants are retired under the resource fence before reissue. Add source-authority indexes for derived grants and resource/assignment-version indexes for approval lookups. Add row checks for nonempty IDs, supported enum values and `ValidUntil > ValidFrom` when present. Cross-module membership/authority references use owner-validated IDs and synchronous views/contracts; do not couple another module to Tenant's writable entity model.

Revoked grants and retired assignments retain audit provenance. Removing a membership cannot make historical records point to a replacement incarnation. Immutable accepted evidence records the authority used at that time; reading that evidence later still requires current access. SQL Server is the implementation/qualification provider now. A future PostgreSQL cutover must replace provider-specific locking/index SQL and repeat the same concurrency tests; the Deal document is not permission to mix providers in one command.

### 3.4 Exact resource Check contract

The following is the proposed neutral contract. It is in-process, not a new publicly accessible HTTP authorization endpoint:

```csharp
public sealed record ResourceAddress(string Kind, string Id);

public sealed record AuthorizationRequest(
    string Permission,
    ResourceAddress Resource,
    Guid? SubjectRevisionId = null,
    Guid? PrincipalParticipantId = null);

public enum AuthorizationDecision
{
    Allowed,
    Denied,
    AuthorityChanged
}

public interface IResourceAuthorization
{
    Task<AuthorizationDecision> CheckAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthorizationDecision> RequireForCommandAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
```

There is deliberately no caller-supplied actor, role, tenant authority, time or `ignoreFilters` flag. The implementation obtains the resolved membership and trusted server time. Resource kinds/keys have generated typed constructors (`Concert(int)`, `Requirement(Guid)`, `ProposalRevision(Guid)`); the wire-neutral address does not change stored key types. The registered descriptor validates key format and required/forbidden subject/principal fields. A participant selector is resolved inside the addressed revision, never accepted as proof of representation. HTTP clients select a business operation; application code selects its permission constant.

`CheckAsync` performs a fresh, bounded database decision and supplies the authority part of UI action hints. The owning stage combines that result with its domain preconditions; an authorized but completed operation need not appear as an available action. The command repeats both checks. Check returns no private resource or ACL and is not a reusable permit. An unknown operation/kind combination fails composition if declared by code and denies if encountered at runtime. Do not expose internal denial reasons or resource existence to unauthorized clients.

`RequireForCommandAsync` requires the owning command transaction to be active, enlists the module's evaluator, acquires its declared authority/resource fences and registers the requirement for final validation. Calling it outside a command transaction is a programming error. Denial marks that transaction unsuccessful even if a caller ignores the returned enum. The application maps the decision into its operation-owned error union. Resource hidden means 404, visible but forbidden means 403, and stale expected versions mean 409; membership/policy changes require a fresh resolution rather than continuing under stale authority.

Internally retain the successful proof's actor/version tuple, permission/catalog revision, resource/subject version, chosen grant/assignment/mandate IDs and versions, represented principal and decision time. That proof belongs to the transaction and is never a bearer token. Final validation checks those dependencies; an unrelated new grant cannot silently replace a revoked mandate in the evidence for a signature. Accepted signatures/approval decisions copy the relevant proof into their immutable audit records. These records explain the past and cannot authorize a later request.

The service permits the full ACL load only after a successful administration decision over minimal resource identity. `resources.share` plus principal/issuer policy is distinct from Summary visibility. The ACL load, mutation, request receipt and outbox use one enlisted privileged context. Being allowed to administer one's issued shares does not entitle the response to disclose other parties' private ACL data.

Each module implements `IResourceAuthorizationEvaluator` keyed by its registered resource kind. It provides: a read predicate builder for each supported operation; the minimal identity/fact queries; and the keys of Tenant, membership, authority, resource and assignment rows the command must fence. It receives a request-bound actor through the neutral port. It never resolves an independent active tenant or calls back through a recipient-filtered repository. One expression/policy definition supplies point checks, query filtering and the authority part of final command validation; there is no separate hand-written `CanX` permission table for the UI.

### 3.5 Queries, filters and scope-safe projections

**Keep the four `ResourceScopedDbContext` stances, replacing their handwritten security predicates with descriptor-driven expression composition.** Do not replace efficient SQL lists with one Check call per row, and do not materialize a user's entire permitted-ID set.

The shared builder owns live membership/version checks, permission-specific audience, recipient/member restriction, validity and source-authority validity. Each module binds its typed grant set, resource key, scope and any operation-specific relationship expression. The builder splices expression bodies, as the current `And` helper does; no compiled delegate or client evaluation may enter the SQL predicate.

For a scope `s` and requested permission `p`, the direct-grant part is:

```text
EXISTS current membership at both resolved authority versions
AND EXISTS grant for this resource, tenant and exact scope s
    WHERE unrevoked AND ValidFrom <= now AND (ValidUntil IS NULL OR now < ValidUntil)
      AND ((audience(p) = TenantResources AND (MembershipId IS NULL OR MembershipId = actor))
        OR (audience(p) = AssignedResources AND MembershipId = actor))
      AND (source authority is absent OR its exact current version is valid)
```

The source-authority condition is grouped inside the grant conjunction. Requirements such as Operations **and** Finance use separate EXISTS clauses; one grant at either scope is insufficient. Scopes are never compared numerically for dominance.

Register a mapped read projection and exact policy for each disclosure surface. The current root defaults remain Application/Booking/Concert Summary, Contract Read, Invoice Read and Thread Read. A different facet uses its own projection root, backed where necessary by a module-owned SQL view over the underlying table, so it is filtered by that facet alone. For example, `ConcertFinance` exposes approved finance columns and uses `settlement.view` + Concert Finance; it does not first traverse the Summary-filtered Concert entity. These are read mappings, not additional writable aggregates or persisted authorization copies. Missing Summary must not accidentally deny an independently granted facet, and a Summary root must not carry hidden finance/PDF fields into a DTO.

At model construction each context supplies registrations; the shared builder installs the named filters. Membership and time remain context-instance/per-query inputs, not captured first-user constants in EF's model cache. A new resource supplies its entity/projection, typed grant mapping and policy registration; it does not paste the audience formula. Existing single-owner tenant filters remain separate. The EF documentation describes context-bound filters and navigation/cycle hazards; SQL translation and projection tests are still necessary. [EF filters][EF1]

**Policy fact queries and ACL administration use internal unfiltered grant mappings.** Remove recipient global filters from the grant fact mappings used by protected projections; a predicate for one operation must not accidentally intersect an already-filtered grant set belonging to a different permission. Resource/projection filters apply the full registered predicate directly to those facts. Recipient-facing grant listings have their own filtered projection. Raw grant sets and full grant navigation loads are restricted to the internal policy/ACL repositories, and never returned through ordinary resource DTOs. Thread participant discovery and recipient delivery similarly require a purpose-specific authorized query, not the viewer's truncated grant set. Grant predicates do not navigate back through a filtered parent.

Apply authorization before count, order and page. The same policy governs detail, list, search, export and attachment retrieval. Mapping code receives only its approved projection. Downloads authorize the exact owning resource/facet and source mandate at delivery time, with no reusable public storage URL for private content. Notifications contain minimal approved data and are checked for each recipient's current access; subscribing to a group does not preserve revoked disclosure.

The owning infrastructure keeps raw DbContexts/IQueryable sources internal. Architecture checks prohibit interactive callers from constructing privileged contexts, using IgnoreQueryFilters, attaching protected entities or executing bulk mutations outside the checked command boundary. EF filters are application enforcement, not database RLS and not protection from arbitrary SQL privileges.

### 3.6 Revocation and in-flight work

Use one local database transaction for the authority decision, resource mutation, receipt and outbox. Cross-module contexts share both connection and transaction, as required by [EF transaction enlistment][EF2]. Standalone reads retain independent connections.

The selected ordering extends the foundation's [command fence][P1]: relevant tenant rows sorted by ID; current actor/target membership rows sorted by ID; Tenant authority-grant rows sorted by ID; resource rows in fixed module/key order; assignment/grant rows; receipts. Read-only authority locks are retained through commit; mutation intent uses update locks from first acquisition. Role administration locks the tenant policy row before editing definitions/assignments. Resource grant revocation takes the same resource lock as consuming commands. Authority-grant revocation takes its authority lock. Discover cross-module keys from minimal facts, acquire them in the declared order, then re-read/validate under the locks; discovery results never authorize a write.

After flushing all domain changes and outbox work, immediately before commit, recheck registered authority requirements against current membership/policy versions, exact mandate/assignment versions and fresh server time. Preserve the checked subject/content version and record the decision time. The final authorization check revalidates authority dependencies, not a pre-transition lifecycle predicate that the command itself has intentionally changed. Domain changes, expected-version checks and final authority validation are separate obligations of the transaction.

Authority-administration commands need an explicit transition rule: editing a role changes RolePolicyVersion itself, and a permitted self-downgrade/removal changes the actor's own membership. These commands authorize the requested transition against locked pre-change authority, retain that evidence and verify at final validation that only their declared mutations account for the changed versions. They still recheck time-dependent prerequisites. They cannot use authority they just granted to authorize another action in the same transaction. All ordinary commands require unchanged authority versions. Test self-removal with another Owner remaining and role edits affecting the editor, so the generic final check neither rolls back legitimate revocation nor creates a self-escalation path.

If revocation wins the fence first, the pending command denies and rolls back. If the command wins and retains its locks, revocation waits and then prevents later commands. A rowversion on the resource alone cannot detect a membership or mandate change. Expiry is evaluated at the final authorization decision: clocks cannot be locked, so do not promise immunity to time passing between that decision and physical COMMIT. SQL checks/read projections reevaluate on execution; long downloads recheck before releasing the next bounded chunk. Bytes already released cannot be recalled.

External payment/blob/publication I/O stays outside database locks. Commit durable authorized intent and a stable operation identity first. A user-dependent operation still awaiting dispatch must revalidate its applicable live authority before becoming irrevocable. Once a provider instruction has been authorized/submitted, reconcile its recorded outcome; a later revocation cannot retroactively unsign an agreement or cancel a completed charge. Accepted principal obligations are distinct from the continued employment/mandate of the person who signed them. System settlement acts through a named internal capability over accepted facts, never a fabricated tenant membership or ambient host bypass.

There is no asynchronous invalidation window to excuse. Async cleanup can remove stale derived grants; authoritative joins enforce denial. Database/authority failures return errors and allow no protected effect. Durable receipts make command retries replay-safe, but replay still checks current disclosure and cannot return an old private response after access was revoked.

### 3.7 Configurable workflow authorization

Each deployed capability declares operation permissions, stage, supported participant slots, minimum input scopes, allowed authority relationship and consequence. Configuration can select and bind these declarations, never author a permission expression.

For EvidenceApproval, a definition binds Respondent and Approver to participants in the exact agreed revision. Submission requires the respondent relationship, `requirements.submit_evidence` and the relevant resource scope. Decision requires `requirements.decide`, the current approver assignment, evidence visibility and the exact current evidence/assignment revision. Publication separately requires `concerts.publish`, the operating principal relationship and all pinned prerequisite outcomes. Finance authority is absent from that chain.

Validate that bindings belong to the revision and that their required access is provisioned when configuring/accepting/assigning. Missing authority is a typed validation failure; the compiler must not silently issue a grant to make a configuration valid. At execution, repeat live checks because membership, assignments and mandates can change. A new supported combination or a new named requirement using the same shape is data. A new authority relationship or obligation semantic is implemented once by its owner and qualified before configurations can select it.

## 4. Permission and relationship authoring

### One authoring source

Introduce a small, build-time `authorization.json` manifest per owning module, with common cross-resource permissions owned by Authorization. It contains permission key, generated symbol, label/category, supported resource policy binding, required scopes, assignable audiences and explicit system-preset grants. A policy binding names a registered, typed C# implementation. The manifest is checked-in developer input, not customer-supplied policy code or a general expression language.

Generate C# constants into the owning Contracts project, registry metadata/preset data for server composition, and the shared TypeScript permission union. Use one deterministic generator with a CI no-diff check. Contracts must not reference Infrastructure to obtain metadata. Customer role-editing screens load the server catalog and need no new checkbox code for each permission. Resource action hints use the same evaluator; the endpoint and command both refer to generated constants.

For example, separating publication from the existing broad `concerts.manage` operation uses:

```json
{
  "name": "concerts.publish",
  "symbol": "Publish",
  "label": "Publish concerts",
  "resource": "concert",
  "policy": "operating_principal",
  "requiresScopes": ["Operations"],
  "assignableAudiences": ["AssignedResources", "TenantResources"],
  "systemPresets": {
    "Owner": "TenantResources",
    "Manager": "TenantResources"
  }
}
```

`operating_principal` is the module's existing typed relationship policy, not a magic predicate invented by JSON. The publication domain operation still evaluates requirements and state. New permission data cannot bypass it. The generated catalog update seeds this explicit grant into system presets and bumps policy versions; custom roles require an Owner to opt in.

### Worked permission: `concerts.publish`

The comparison assumes publication's endpoint/service already exist, as they do at this HEAD, and compares an independently enforced permission, not a bare unused constant. Existing P1 repair work is excluded from both counts.

| Authored path/change | Current shape | Selected shape |
|---|---:|---:|
| Permission declaration / owning manifest | TenantPermission.cs: 1 | Concert/authorization.json: 1 |
| Explicit Owner/Manager bundle edits | PermissionCatalog.cs: 1 | Generated: 0 |
| Endpoint permission reference | ConcertController.cs: 1 | Same controller: 1 |
| Command authorization reference | ConcertService.cs: 1 | Same service's RequireForCommandAsync request: 1 |
| Client permission union | app/shared/src/features/tenant/types.ts: 1 | Generated: 0 |
| Behavioral allow/deny/revocation coverage | One existing publication integration test file: 1 | One equivalent integration test file: 1 |
| **Total hand-authored files** | **6** | **4** |

The endpoint currently uses `ConcertsManage` at [ConcertController.cs:173–178][C24]; `PostAsync` does not yet have the target command check. This table is a concrete design comparison, not a claim that today's endpoint is already safely fenced. No DbContext, grant enum, role enum, mapper or policy-provider edit is needed merely to add this permission over an existing policy/scope. Generated C#/TypeScript/metadata changes still appear in the resulting diff where generated artifacts are checked in; the reduction is in independent authoring sites, not concealed output.

For an existing operation's customer-specific bundle, the target requires **zero source-file changes**: Owner creates/edits a role through Tenant, assigns it to a membership, and the new policy version takes effect. This is the larger improvement over today's code-only six-role table.

### Worked relationship: a named requirement reviewer

Add `ApprovalAssignment` linking a requirement's bound approver participant to a particular current membership. This is a new relationship type with its own lifecycle, not another global job title. Use the existing EvidenceApproval resource, evidence read scope and `requirements.decide` permission. The member must belong to the bound approver tenant; the relation carries version/validity/revocation; the reviewer can decide only the current evidence revision. Reassignment revokes the prior assignment, invalidates current satisfaction and leaves previous decisions immutable.

The following file budget fixes its boundary: Requirement/EvidenceRevision, their read/decision endpoints and the decision permission already exist; this slice adds **named reviewer assignment/revocation**. Paths below are proposed under the Concert module, not claims that P5 is implemented now.

| End-to-end change | Authored files in either implementation |
|---|---:|
| ApprovalAssignment entity and EF configuration | 2 |
| RequirementEntity's assignment/reassignment behavior | 1 |
| Context mapping/DbSet and requirement repository + its interface | 3 |
| Assignment request and requirement service + its interface | 3 |
| Requirement controller assignment/revocation endpoints | 1 |
| Integration scenarios exercising assignment, revision, membership and revocation races | 1 |
| **Shared feature cost** | **11** |

With today's organization, the read-filter expression goes into the context and the command/action predicate into the service: both are already in those **11 authored files**. With the selected design, add **one `RequirementAuthorizationPolicy.cs` binding** used by Check, exact-scope filtering and fenced commands: **12 authored files**. The selected model adds an explicit policy file; it does not magically remove the domain feature's files. It removes independently authored security logic from the context/service, whose changes become mapping and policy consumption. Add the assignment's structural SQL through the repository's InitialCreate regeneration in both cases; generated migration/designer/snapshot artifacts are additional, not hand-authored policy sites. If the feature exposes a new reviewer-selection UI, its component/API/hook changes are additional in both cases. These counts are scoped implementation budgets, not a measured future diff or a claim that every new relationship costs twelve files.

The new policy binding supplies its SQL relation expression and fence dependency on the requirement/assignment; registers the exact input revision contract; and composes existing membership, permission, audience, grant and validity primitives. Other modules and the six system-role definitions do not change. A new relationship involving a new persistent concept still needs that concept's real domain/storage/UI work. No authorization library makes those files disappear.

### Composition checks that prevent recurrence

Fail composition or CI for duplicate/unknown keys; missing evaluator/resource/scope registrations; generated catalog drift; an endpoint or capability naming an undeclared permission; and an exposed private projection without its policy registration. For each declared `(preset, permission, audience, resource policy)` supply at least one valid positive scenario and the relevant negative matrix. An AssignedResources path must have a real assignment issuer/consumer exercised by integration tests. Static metadata alone cannot prove reachability.

Include multi-role audience union, tenant switching on one cached EF model, scope-only grants, member removal/rejoin, role-definition changes and direct service invocation without an HTTP attribute. Run Check/list/detail/action-hint consistency scenarios against the same policy. This catches R22's unreachable messaging promise and the 22-versus-19 contract drift without merely snapshotting a repeated role table.

The infrastructure investment is real: manifest generation, policy registration validation, evaluator/query composition, role administration and fence integration must be built and tested once. A four-file marginal permission change is the outcome of that work, not an estimate for the whole redesign.

## 5. Delivery position and what P1 changes now

P1 is mid-flight and has open critical findings. It must continue its authorized repair work. Do not block those repairs on a graph/policy-engine experiment, a role editor or P2's party schema. Equally, do not declare this decision satisfied by creating another follow-on placeholder.

| Boundary / accountable code owners | Concrete work and exit condition |
|---|---|
| **Current P1 repair: Authorization, DataAccess and current resource owners** | Repair R1 through the privileged ACL path and real command transaction; centralize the nine audience branches; introduce the resource Check/fence contract over current grants and route repaired operations through it. Restore/discover/run the Authorization test project and close the other critical review findings. Qualify actual SQL translation and both revocation race orders. No new resource module copies a predicate. |
| **Complete role composition immediately after the safe P1 boundary, before admitting P2's new participant operations: Tenant + Authorization + shared clients** | Replace single-role storage/contracts with definitions/assignments and role policy versions; deliver Owner-managed role creation/assignment and generated permission authoring, including invite and last-owner behavior. Prove a custom production role combined with a finance role, permission-specific audience union, and in-flight role downgrade denial. This is required work in the foundation delivery chain, not an optional unowned plan. |
| **P2 participant consumers: Show, entry, Booking, Concert** | Consume the same policy/query/fence API for explicit principal/revision/slot relationships and independent scopes. Replace fixed pair-based actor predicates with the new participant bindings. Prove promoter/venue/artist separation and headliner/support privacy. |
| **P3 representation: Tenant + entry/Booking/resource owners** | Add exact-version mandate checks and synchronous derived-access validity. Prove revocation/removal/expiry against consent, final acceptance and downloads before exposing signing. No collection or transitive delegation by implication. |
| **P4/P5 consumers: DirectInvitation + Concert** | Use the existing authority boundary for addressed offers and requirement assignment/decision. Prove both entry routes, stale evidence/reviewer changes and publication consequences without finance disclosure. Capability configuration consumes these implemented contracts. |

The role-composition boundary has a named implementation owner in the module sense, a position and observable completion criteria. It does not require another research/design plan before implementation. P1 can reach a safe local checkpoint first; P2 cannot start building new user-facing authority assumptions on the six-role storage shape. These are dependency boundaries, not a prescription for one PR each, and this research authorizes no push or PR.

Specific instructions for the P1 implementer now:

1. Replace the provisional RBAC-deferral paragraph in the implementation plan with a link to this decision when reconciling that plan. Keep the four-stage route and existing scope contracts. This research commit changes only this decision document.
2. Treat `IPermissionCatalog`'s role lookup as the current input to the common policy, never a switch statement to copy into a new module. Move permission/audience evaluation behind the shared operation contract so composable roles replace that input without rebuilding every resource policy.
3. Fix ACL administration with complete, internally loaded grant facts after current actor/principal authorization. A method called `GetIdentityByIdForUpdateAsync` is not proof of a SQL update lock: the inspected implementation is an ordinary LINQ query. Implement and test the actual fence. [Repository][C16]
4. Make Staff messaging truthful within P1. Implement explicit conversation member assignment/revocation through the Conversations owner, granting Read and SendMessages to the exact current incarnation only after conversation administration authority. A Concert assignment must not expose every associated private discussion. If that conversation capability is not in the current repair slice, remove Staff's messaging grants for that slice and restore them only with the functioning, tested path before P1 is claimed complete; this is a sequencing constraint, not permission to leave an unreachable catalog entry.
5. Keep denied effects, expired-share reissue and receipts atomic, and validate authority on replay. Close R2/R20 and the other active P1 acceptance failures under their existing review ownership; fixing the policy design does not fix their call sites.

No production data, deployed consumer or retained-record need was established or assumed. The task explicitly establishes a pre-launch replacement: delete superseded role fields, duplicated catalogs/predicates and obsolete clients together, and regenerate synthetic data/InitialCreate as required. No dual policy evaluation, compatibility role, old-envelope reader or backfill is part of this decision. The older central expressiveness document still contains conditional retained-data/legacy-adapter prose and a user-ID-based grant example; the explicit task constraints and this incarnation-based implementation decision supersede those details for this work. Its product separation of authorities remains applicable.

## 6. Qualification and remaining questions

Before any new authority surface is considered complete, demonstrate the following against the current database provider:

- Same user in two tenants cannot combine permissions; absent/malformed/foreign tenant context fails appropriately. A removed and rejoined member cannot use an old resource grant, reviewer assignment or unaccepted consent.
- Every allowed preset/custom-role combination has an exercisable grant path. Multiple roles widen only the relevant permission's audience. Owner and Finance in unrelated tenants receive no resource access.
- Summary-only, operations-only, finance-only, terms-only and conversation read/send-only cases expose exactly their independent contract. Counts, exports, nested projections, messages and private downloads obey the same rules.
- ACL administrators can find their issued grants, revoke/reissue them and replay safely; recipients cannot obtain or administer the full ACL. Authorized thread participant queries remain complete without disclosing unrelated ACL details.
- Two-connection races exercise both orders for member removal, role change, role-definition edit, grant expiry/revocation, mandate replacement, reviewer reassignment and evidence revision. Assert committed effects, receipts and outbox rows, not merely a boolean Check result.
- Agency signs for the selected principal with explicit terms disclosure; no money collection follows. Revocation blocks future delegated work and stale unaccepted consent, preserving accepted historical evidence.
- Independent requirements affect only their agreed consequences. Revised rider evidence cannot approve itself, alter fees or authorize settlement. Public publication data excludes private terms.

Measure the point-check, page/count and commit-fence SQL separately. Start with the product document's **synthetic** design cases: one million engagements, five million live grants, typical 2–20-party shows and a 100-party stress show, including a large tenant with 100,000 accessible engagements. Record actual plans, logical reads, p95/p99 latency, lock waits and revocation duration. These are qualification inputs, not observed traffic or a promised capacity. Agree numerical release budgets from the measured service baseline before calling the implementation qualified. [Product query assumptions][D3]

| Open evidence question | What closes it / current decision impact |
|---|---|
| Are all detailed Muzeek help behaviors unchanged in the current product? What are the exact permission toggles, custom-role and roster interaction rules? | Tommy supplies the Muzeek version/account he means and a walkthrough or permission-matrix export from a test account. Public help, product and developer pages were inspected; no authenticated Muzeek behavior was tested. This may refine UX/parity scope, not the need to separate member authority from cross-business relations. |
| Will real customers require nested groups, transitive delegation or arbitrary customer-authored access rules? | A concrete arrangement with required delegation depth, revocation expectations and privacy boundaries. Existing product documents specify no initial chains. The selected model supports the documented one-hop cases; do not prebuild recursive policy machinery. |
| What authority/disclosure is acceptable for a full roster portfolio, and which acts may a representative perform? | Product-approved act/scope matrix and consent requirements from representative customer arrangements. Restricted/full portfolio implementation must use explicit resource-owner policies; P3 remains signing-only. |
| What evidence legally suffices for a signing or future collection mandate, and how long is it retained? | Product/legal owners decide evidence and retention rules. The technical model records versions/provenance but does not establish legal sufficiency. No new legal conclusion is made by this document. |
| What are actual load, acceptable lock contention and provider-cutover dates? | Database benchmarks and the provider delivery owner. Current evidence supports a shared SQL Server transaction; PostgreSQL must be qualified when adopted. No engine benchmark was run. |
| Which central guidance details should be reconciled with the current pre-launch foundation? | The docs owner updates the pinned document's stale implementation/migration examples against this decision and the current foundation. All three required central product documents were retrieved; missing access is not being used as a design gap. |

An external graph engine becomes worth a new decision if authoritative relationships must span independently deployed services/datastores, customer cases require recursive usersets/delegation, or measured indexed SQL fails agreed budgets. Those are observable changes to the present evidence. They do not postpone the selected in-process implementation, custom roles or any current revocation requirement.

## Source register

Local evidence links are immutable source snapshots with file/line anchors. Product references are pinned to the fetched docs revision; vendor references were checked on 18 September 2026. Proposed types, file budgets and architecture choices are recommendations in this document, not assertions that they already exist.

[C1]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Authorization/Concertable.B2B.Authorization.Infrastructure/Services/MembershipContext.cs#L28-L82
[C2]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Authorization/Concertable.B2B.Authorization.Infrastructure/Authorization/PermissionAuthorizationHandler.cs#L23-L28
[C3]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Authorization/Concertable.B2B.Authorization.Contracts/Enums/TenantRole.cs#L7-L15
[C4]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Tenant/Concertable.B2B.Tenant.Domain/Entities/TenantMembershipEntity.cs#L16-L50
[C5]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Authorization/Concertable.B2B.Authorization.Contracts/TenantPermission.cs#L10-L31
[C6]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Authorization/Concertable.B2B.Authorization.Infrastructure/Authorization/PermissionCatalog.cs#L9-L84
[C7]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Application/ResourceAccessGrant.cs#L15-L74
[C8]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Application/ResourceGrantKind.cs#L3-L8
[C9]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/ResourceAccessExpressions.cs#L8-L28
[C10]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/MembershipAuthorityConfiguration.cs#L8-L17
[C11]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Data/ConcertDbContext.cs#L34-L70
[C12]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Booking/Concertable.B2B.Booking.Infrastructure/Data/BookingDbContext.cs#L20-L49
[C13]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Application/Concertable.B2B.Application.Infrastructure/Data/ApplicationDbContext.cs#L20-L43
[C14]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Conversations/Concertable.B2B.Conversations.Infrastructure/Data/ConversationsDbContext.cs#L24-L63
[C15]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Concert/Concertable.B2B.Concert.Domain/Entities/ConcertEntity.cs#L106-L227
[C16]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Repositories/ConcertPrivilegedRepository.cs#L17-L30
[C17]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Conversations/Concertable.B2B.Conversations.Domain/Entities/ThreadEntity.cs#L23-L54
[C18]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/Directory.Packages.props#L27-L50
[C19]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Concertable.B2B.DataAccess/Concertable.B2B.DataAccess.Infrastructure/ResourceAccessGrantConfiguration.cs#L18-L51
[C20]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/app/shared/src/features/tenant/types.ts#L8-L34
[C21]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Authorization/Tests/Concertable.B2B.Authorization.UnitTests/PermissionCatalogTests.cs#L15-L69
[C22]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Services/ConcertService.cs#L292-L451
[C23]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure/Repositories/ConcertRepository.cs#L31-L34
[C24]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Concert/Concertable.B2B.Concert.Api/Controllers/ConcertController.cs#L173-L178
[R1]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/reviews/Refactor-PartyFoundationLegacyBindings.md#L295-L592
[P1]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/plans/party-foundation/PARTY_FOUNDATION_PLAN.md#L746-L917
[P2]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/plans/party-foundation/PARTY_FOUNDATION_PLAN.md#L297-L357
[P3]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/plans/party-foundation/PARTY_FOUNDATION_PLAN.md#L1748-L1776
[P4]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/plans/party-foundation/PARTY_FOUNDATION_PLAN.md#L1804-L1848
[P5]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/plans/party-foundation/PARTY_FOUNDATION_PLAN.md#L1439-L1555
[P6]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/plans/party-foundation/PARTY_FOUNDATION_PLAN.md#L1557-L1594
[P7]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/plans/party-foundation/PARTY_FOUNDATION_PLAN.md#L1385-L1397
[D1]: https://github.com/Concertable/docs/blob/466910f82dabf1b20ec1cf6e20a89f3351813d74/product/CONFIGURABLE_DEAL_WORKFLOWS.md#L88-L193
[D2]: https://github.com/Concertable/b2b/blob/e5263bb44f7b81fb145869ba85d3ff92372b0c8e/api/src/Modules/Deal/ARCHITECTURE.md#L315-L441
[D3]: https://github.com/Concertable/docs/blob/466910f82dabf1b20ec1cf6e20a89f3351813d74/product/CONFIGURATION_EXPRESSIVENESS.md#party-and-authority-design
[D4]: https://github.com/Concertable/docs/blob/466910f82dabf1b20ec1cf6e20a89f3351813d74/product/NIGHTCLUB_SETTLEMENT_CASE_STUDY.md#questionnaire-and-change-variant
[D5]: https://github.com/Concertable/docs/blob/466910f82dabf1b20ec1cf6e20a89f3351813d74/product/CONFIGURABLE_DEAL_WORKFLOWS.md#L207-L353
[M1]: https://help.muzeek.com/en/articles/2260937-adding-more-team-members-to-your-account
[M2]: https://help.muzeek.com/en/articles/5644554-transfer-ownership-of-your-account
[M3]: https://help.muzeek.com/en/articles/2324133-using-rosters-for-agencies-venue-groups-management-companies-and-other-collectives
[M4]: https://help.muzeek.com/en/articles/5248050-restricted-and-full-access-permissions-for-rostered-accounts
[M5]: https://muzeek.ai/developers#permission-scopes
[M6]: https://muzeek.com/
[M7]: https://muzeek.ai/pricing
[Z1]: https://research.google/pubs/zanzibar-googles-consistent-global-authorization-system/
[F1]: https://openfga.dev/docs/modeling/conditions
[F2]: https://openfga.dev/docs/modeling/organization-context-authorization
[F3]: https://openfga.dev/docs/interacting/consistency
[S1]: https://authzed.com/docs/spicedb/concepts/caveats
[S2]: https://authzed.com/docs/spicedb/concepts/expiring-relationships
[S3]: https://authzed.com/docs/spicedb/concepts/consistency
[E1]: https://docs.cerbos.dev/cerbos/latest/recipes/filtering-resources.html
[E2]: https://www.openpolicyagent.org/docs/filtering/tutorial-sql-filtering
[E3]: https://www.openpolicyagent.org/docs/filtering/fragment
[E4]: https://v3.casbin.org/docs/rbac-with-domains
[EF1]: https://learn.microsoft.com/en-us/ef/core/querying/filters
[EF2]: https://learn.microsoft.com/en-us/ef/core/saving/transactions#cross-context-transaction
