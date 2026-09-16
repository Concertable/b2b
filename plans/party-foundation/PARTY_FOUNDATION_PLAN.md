# Party foundation implementation plan

## 1. Outcome, scope and acceptance boundary

Replace B2B's fixed venue/artist access model with explicit participation, resource access and
accepted responsibilities. A promoter can organise a show at someone else's venue, book independently
contracted acts through either entry route, and give a production member access to assigned work
without disclosing fees. An agency can sign for an artist while payment still goes to the artist.

The product target is open-ended composition of supported work: who does what, when, with which
evidence, approval and consequence. Participant cardinality must not require new tenant columns.
New arrangements composed from implemented capabilities become data; new semantics still require
code. The lifecycle routes remain:

- Opportunity → Application → Booking → Concert.
- Direct Invitation → Booking → Concert.

This plan implements the foundation, both entry routes, and one enforced evidence/approval
capability. Deal Configuration owns the economic language, configuration persistence and configured
capability selection. Its later authoring surface, the six-obligation-shape research, cross-booking
predicates, agency collection, multi-leg settlement, paid-agreement amendments, room reservations and
automated external integrations remain explicitly separate work. Section 12 maps those extensions to
the foundation; completing this plan does not claim the entire nightclub scenario executes.

**Nothing is live:** no production data, deployment or users require preservation. Replace schemas,
DTOs and internal messages directly; regenerate InitialCreate and synthetic fixtures. Do not introduce
adapters, dual readers/writers, old columns for compatibility, backfills, environment-conversion
manifests or a parallel event version. Future agreements created by the replacement model are
immutable business records; that requirement does not justify retaining today's fixed-pair shape.

The replan is complete and reviewed. All five implementation phases below remain outstanding, and
P1 is the next implementable slice. Current execution state belongs to
[the companion ledger](PARTY_FOUNDATION_PROGRESS.md).

### Product authority and corrections to older prose

Read the current source with these product documents:

- [Configurable workflows](https://github.com/Concertable/docs/blob/main/product/CONFIGURABLE_DEAL_WORKFLOWS.md):
  fixed routes, multi-profile businesses, stage-owned capabilities and customer configuration.
- [Configuration expressiveness](https://github.com/Concertable/docs/blob/main/product/CONFIGURATION_EXPRESSIVENESS.md):
  separate participation, signing, payment, approval and visibility.
- [Nightclub benchmark](https://github.com/Concertable/docs/blob/main/product/NIGHTCLUB_SETTLEMENT_CASE_STUDY.md):
  multi-agreement grouping, changed evidence, advances and greater-of economics.
- [Deal target](../../api/src/Modules/Deal/ARCHITECTURE.md#5-configurable-deals--target-design),
  [legal requirements](../../api/src/Modules/Deal/LEGAL_REQUIREMENTS.md),
  [Concert guidance](../../api/src/Modules/Concert/AGENTS.md) and
  [structural rosters](../../CODE_PATTERNS.md).

The central party document still describes the rejected adapter-first sequence and retained-data
conversion. Those implementation recommendations are superseded by the founder's explicit rejection
and this replan. Its separation of authorities and disclosure requirements remains applicable.
The legal document's old ABSENT labels, Concert-owned acceptance and greater-of Versus wording
conflict with source. They are documentation debt, not instructions to recreate those behaviours.
No legal rates, retention periods or sufficiency of agency evidence are decided by this engineering plan.

## 2. Source baseline and branch disposition

Inspected on 15 September 2026. These are source observations, not newly executed runtime tests.

| Source | Established fact and design consequence |
|---|---|
| B2B branch Refactor/PartyFoundationLegacyBindings on cached origin/main 2b5264b4c9748931c840c816e99408d537bec7c2 | Branch restarted 15 September 2026. The runtime tree is identical to origin/main; the only local commit carries this plan, its roadmap, ledger and review work order. No branch PR |
| DataAccess.Application/IVenueArtistTenantScoped.cs; DataAccess.Infrastructure/TenantFilters.cs and VenueArtistTenantInterceptor.cs | Pair predicate is IsHost OR matching VenueTenantId OR matching ArtistTenantId. Interceptor validates non-empty/unchanged pair values, not general command authority |
| ApplicationDbContext, BookingDbContext, ConcertDbContext, ConversationsDbContext | All nine entities in section 4 receive pair filtering. ReadDbContext and privileged alternatives remain separate escape surfaces to qualify |
| Tenant.Infrastructure/Services/TenantContext.cs and Tenant.Contracts/PermissionCatalog.cs | Active membership is database resolved; permission catalog dispatches on single TenantType; no HttpContext grants IsHost bypass |
| Application.Infrastructure/Services/ApplicationWorkflow.cs | Submission records artist signature/fingerprint. Acceptance reloads mutable Deal/profile facts, invokes an application-specific snapshot and has a fresh-scope acceptance rerun |
| Booking.Infrastructure/Services/BookingWorkflow.cs and BookingEntityConfiguration.cs | Booking requires ApplicationId/OpportunityId, uniquely indexes ApplicationId/OperationId, mints Contract, then initiates confirmation. Verification evidence also identifies ApplicationId |
| Application/Booking UnitOfWork and their context registrations | Each module registers its own SQL Server context connection. A pre-commit ApplicationAcceptedDomainEvent invokes BookingWorkflow; nested units of work alone do not prove one transaction |
| Booking.Contracts/ConfirmedBooking.cs; Concert's BookingConfirmedIntegrationEventHandler | Asynchronous confirmed handoff contains only fixed profile/tenant identities and typed economics. Concert creation consumes it after confirmation |
| ConcertEntity, InvoiceIssuer, TicketSaleProcessor and PaymentOperationReferences | Financial direction comes from the pair; native sales currently increment a count; setup/verification references embed Opportunity/Application identities. Both identity and outcome correlation must change for invitations |
| Concert.Contracts/Events/ConcertChangedEvent.cs | Actual cross-repo event includes public listing facts plus PayeeUserId/PayeeOwnerId. Private participants must never enter this projection |
| Current solution and CI | net10.0, SQL Server. The earlier 533 unit/architecture passes and green build covered the discarded runtime commits and are not evidence for any phase below |

Abbreviated module paths above are under api/src/Modules; DataAccess is under
api/src/Concertable.B2B.DataAccess. The branch and dependency evidence is detailed in sections 10–11.

### The branch was restarted; P1 starts from origin/main

The two rejected implementation/remediation commits and the rejection brief were dropped on
15 September 2026. Nothing was pushed, so no history outside this checkout's reflog referenced them.
The branch now carries only this plan, its roadmap, ledger and review work order on top of
origin/main; every `api/` path is identical to origin/main.

`LegacyFinancialParties` and its five-role vocabulary do not exist. Do not recreate that value, and do
not recover the dropped commits to resume, cherry-pick or credit them as a completed foundation phase.

P1 therefore starts from the shape origin/main actually has: `IDealPayeeResolver` in
Concert.Application, whose `DealPayeeResolver` facade selects `VenuePaysArtistDealPayeeResolver` or
`ArtistPaysVenueDealPayeeResolver` per `DealType` and returns the ticket user, ticket tenant or
settlement tenant. That keyed family is the live starting point, not a deleted one; Concert/AGENTS.md,
Deal/ARCHITECTURE.md and Deal/LEGAL_REQUIREMENTS.md are at their origin/main text and describe it
correctly today. P1 updates those three documents to match what P1 itself implements.

P1 replaces the pair-derived financial direction as part of resource access. The small direction
expression may stay inline in the real financial consumer until P2 supplies accepted payer/payee
bindings; it earns no compatibility class, exported contract or separate phase.

## 3. Names, owners and dependency direction

Keep the Concert module's reservation: **InvoiceParty** is the invoice's frozen legal-side value.
Use **Participant** for the new relationships: ShowParticipant, AgreementParticipant,
RevisionParticipant, ParticipantBinding and ParticipantSlotDefinition. Update Concert guidance's
two-tenant premise when P1/P2 replace it; do not silently expand the reserved Party vocabulary.

Tenant remains the legal/business/membership/settlement identity. Artist and Venue identify marketplace
profiles; VenueId selects the location profile, not the organiser or payer. A promoter needs no invented
Venue record. Do not add Organisation/Party aggregates or PromoterTenantId columns.

| Owner | Responsibility and dependency |
|---|---|
| Tenant | Tenant legal/contact/eligibility facts, optional business profiles, memberships and their revisions; representation grant storage and issuance. Depends on Authorization.Contracts and implements its membership/authority facts port |
| New Authorization.Contracts / Authorization.Infrastructure | Contracts owns TenantRole, TenantPermission, IMembershipContext and the membership-fact port; Infrastructure owns request resolution, permission catalog, policy handler and explicit system execution. Neither depends on Tenant assemblies; no aggregate or database of its own |
| B2B.DataAccess | Reusable query predicate composition, transaction enlistment and write-fence mechanics. Does not infer resource policy or load another module's aggregate |
| Each resource module | Its own typed grant tables, ownership/share policy, filtered queries, facet DTOs, resource action checks and grant mutation commands |
| New Show module | Coordination identity, participation, schedule revisions, booking slots and claims, grouping projection and a native-publication claim. Never advances all child lifecycles |
| Application / new DirectInvitation | Sole writer of its proposal head, agreement participant identities, immutable proposed revisions and pre-acceptance consents |
| New Agreement.Contracts | Immutable, cross-stage AgreementSnapshot, entry origin, participant/binding/consent values and AcceptedAgreement. Depends on Deal value contracts and Payment reference contracts; never on stage entities or workflows |
| Deal | Pure economic terms and finite descriptors for economic participant slots. Current four arrangements remain registered presets until its configuration owner replaces the economic model |
| Booking | Contract and accepted revision storage, funding/confirmation, cancellation and outstanding confirmation obligations |
| Concert | Per-engagement operational lifecycle, publication, evidence proof, settlement and invoices. Consumes accepted facts; never reopens the entry or reloads a live Deal |
| Payment | Provider operations, accounts, commission, balances and recovery; receives authorised instructions using its existing owner IDs and opaque client references |

Agreement.Contracts must have a publishable dependency closure if exposed by existing packable stage
contracts. Publish it with the B2B package set; internal callers change together via ProjectReference.
Packability alone does not create an external consumer or require additive APIs. Concert's external
publication contracts never depend on Agreement.Contracts.

P1 moves the existing role/permission/request-authority contracts out of Tenant.Contracts; there are
no duplicate enums or forwarding types. Tenant business-profile kinds stay in Tenant.Contracts and
are consumed by the owning operation's eligibility policy, not by Authorization's membership port.
Composition roots connect Tenant's fact implementation to Authorization. This makes the dependency
direction compile without a Tenant↔Authorization cycle.

### Two economic identities, named for their present job

The initial SettlementBinding contains **PayerParticipantId** and **PayeeParticipantId**, referencing
principals in the exact revision. It describes one supported direct transfer. No BeneficialRecipient,
Collector, receipt-mandate placeholder or five-field direction value is introduced.

For this admitted single-supply capability, supplier is the payee and customer is the payer.
Invoice construction consumes that binding and freezes InvoiceParty values at the applicable tax point.
Ticket-sale entitlement is an explicit publication selection; the current presets select their payer.
It is not inferred from the venue location or the user who clicked Accept.

A participant list may contain any supported number of principals, representatives and operational
participants. Only this financial capability requires one payer and one payee. Later multi-leg or
collection capabilities add their own validated contracts; they do not widen a hidden global
two-participant rule.

## 4. Resource access comes first

P1 replaces the complete pair-scoped access implementation. Its observable result is a separately
owned business seeing an expressly shared concert summary while being unable to read its agreement,
invoice, private messages or financial actions. This can use existing Concert/Booking IDs; Show and
agreement revisions are not prerequisites for granting access to those resources.

### Schema replacement and the nine-entity inventory

Every grant row has an identity, its real module-local resource FK, TenantId, optional MemberUserId,
a finite Facet, ValidFrom/ValidUntil, RevokedAt, issuer identity, origin and bigint Version.
P3 adds an optional exact signing-grant source. A member-specific row narrows the tenant audience:
it still requires that user's current membership in that tenant. It does not require a second
tenant-wide row, which would disclose the same resource to other members.

| Existing entity | Replacement ownership/access shape |
|---|---|
| ApplicationEntity | ApplicationAccessGrant with Summary/Proposal facets; P2 adds per-revision terms grants. Existing principal facts initially drive explicit initial grants, never a fallback query predicate |
| BookingEntity | BookingAccessGrant with Summary/Operations facets. Accepted terms stay on ContractRevision grants |
| ContractEntity | ContractAccessGrant in P1; replaced by ContractRevisionAccessGrant when P2 creates immutable accepted revisions. A booking-summary grant never grants a contract download |
| ConcertEntity | ConcertAccessGrant with Summary/Operations facets; finance DTOs require Finance separately. No venue/artist visibility comparison |
| InvoiceEntity | InvoiceAccessGrant with Invoice facet; its two legal InvoiceParty snapshots replace redundant pair scoping columns |
| ConcertAvailabilityEntity | Internal rebuildable availability projection, no private generic repository or pair marker. Public/facade output is only availability/conflict, not another booking's parties, fees or IDs |
| MessageEntity | New ThreadEntity and ThreadAccessGrant; Message has a ThreadId FK, SenderTenantId and SentByUserId. Thread audience, not pair columns, governs messages |
| ThreadReadStateEntity | ThreadId FK + TenantId + UserId, unique triple; monotonic per-member watermark. Read/write requires matching membership and thread grant |
| ContentReportEntity | Retain MessageId FK and reporter/reported identities; typed report grants for the reporter's status and authorised moderation. Thread membership does not disclose reporter evidence or internal resolution notes |

Delete IVenueArtistTenantScoped, the pair repository interfaces/bases/specification, TenantPair,
ApplyVenueArtist and its interceptor. Remove their registrations and generic constraints, not just
entity markers. Actual single-owner profile data keeps single-owner filtering. Inventory all usages
rather than treating the direct-reference count as the full changed surface.

P1 replaces access while the current two-principal economic model still executes. Remaining
VenueTenantId/ArtistTenantId fields in entry/accepted financial data are inputs to that existing domain,
not alternate access readers or a retained schema version. P2 replaces those fields in the same change
as their financial consumers. This separation lets third-business access ship first without admitting
unsigned third-business financial rights.

Opportunity retains a managing TenantId for authoring and a separate published opportunity projection
for browsing. Deal's editable offer is not a public API for arbitrary private terms. All typed grants
and new parent/child relationships use real FKs within the owning module and restricted deletion;
cross-module tenant/profile identities are checked through contracts, not aggregate EF navigations.

### Authoritative read and write predicate

An ordinary private read requires:

1. Authenticated human and resolved active-tenant membership.
2. Member permission for the requested facet.
3. A matching, unrevoked resource grant, including member restriction and time validity.
4. A current membership revision; from P3, a live exact authority source for a derived grant.

Implement module-owned EXISTS predicates with context-instance tenant/user/time values. The same SQL
statement verifies the membership is still present at the resolved revision. Tenant owns a narrow
read-only authority-facts relation; Authorization/DataAccess may map this expressly owned cross-schema
policy relation, with no writes or EF navigation into Tenant's aggregate. A stale membership revision
denies the read and requires resolution again. Do not materialise every authorised resource ID.

Filters are acyclic: grants never navigate back through a filtered parent. Parent filters protect
entity access; specialised projections apply their own facet predicates before selecting fields.
Includes, child queries, scalar projections, counts and exports must obey the same contract.
Use separate operations/terms/finance/evidence responses; do not return a full DTO and hide its fields
in React. Public endpoints use bounded approved publication DTOs, never filter-disabled private details.

Grant creation/revocation requires the resource's sharing policy plus the member's sharing permission.
Current direct principals receive explicit grants at resource creation. They retain access to their
executed record through current membership after leaving a show. A show manager cannot widen another
agreement's disclosure policy. P2 records the principals and consents required for such a widening.

The sharing policy is a rule on the owning aggregate, not a sharing service. Grant `Issue` stays
`internal`, so the aggregate remains the only issuer; `Share`/`RevokeShare` sit beside the existing
private `IssuePrincipalGrants` and carry `GrantOrigin.ExplicitShare`:

```csharp
// Concert.Domain/Entities/ConcertEntity.cs — the policy, as code
private static readonly FrozenSet<ConcertAccessScope> ShareableScopes =
    FrozenSet.ToFrozenSet([ConcertAccessScope.Summary, ConcertAccessScope.Operations]);

public Result<ConcertAccessGrant, ShareConcertError> Share(
    Guid toTenantId, Guid? toMemberUserId, ConcertAccessScope scope,
    Guid byTenantId, Guid byUserId, DateTime at, DateTime? validUntil = null)
{
    if (!ShareableScopes.Contains(scope))
        return new ShareConcertError.ScopeNotShareable(scope);      // Finance is never disclosed by share
    if (byTenantId != VenueTenantId && byTenantId != ArtistTenantId)
        return new ShareConcertError.NotAPrincipal(byTenantId);     // a shared-to tenant cannot re-share
    if (accessGrants.Any(g => g.TenantId == toTenantId && g.MemberUserId == toMemberUserId
                              && g.Scope == scope && g.IsLiveAt(at)))
        return new ShareConcertError.AlreadyShared(toTenantId, scope);

    var grant = ConcertAccessGrant.Issue(Id, toTenantId, toMemberUserId, scope,
        byTenantId, byUserId, GrantOrigin.ExplicitShare, at, validUntil);
    accessGrants.Add(grant);
    return grant;
}

public Result<Unit, RevokeShareError> RevokeShare(Guid grantId, Guid byTenantId, DateTime at)
{
    if (accessGrants.SingleOrDefault(g => g.Id == grantId) is not { } grant)
        return new RevokeShareError.GrantNotFound(grantId);
    if (grant.Origin != GrantOrigin.ExplicitShare)
        return new RevokeShareError.NotAShare(grantId);             // a principal's own access is not revocable here
    if (grant.IssuedByTenantId != byTenantId)
        return new RevokeShareError.NotTheIssuer(byTenantId);

    grant.Revoke(at);
    return Unit.Value;
}
```

The member's sharing permission is the existing `TenantPermission.ResourcesShare`, checked in the
application layer before the aggregate is loaded — the aggregate owns disclosure policy, the catalog owns
who may act:

```csharp
// Concert.Application — the command, not a ConcertSharingService
if (!membership.HasPermission(TenantPermission.ResourcesShare))
    return new ShareConcertError.NotPermitted();
```

HTTP surface, per resource module that has a grant family:

```
POST   /concerts/{id}/shares            { toTenantId, toMemberUserId?, scope, validUntil? } -> 201 grant
DELETE /concerts/{id}/shares/{grantId}                                                      -> 204
```

Which scopes each family will disclose, so the rule is one table rather than six inventions:

| Family | ShareableScopes | Why |
|---|---|---|
| ConcertAccessGrant | Summary, Operations | Finance is withheld: an operational participant runs the show without seeing what anyone is paid |
| BookingAccessGrant | Summary, Operations | The same engagement one stage earlier |
| ApplicationAccessGrant | Summary | Proposal is the applicant's own pitch; disclosing it is the applicant's act, not a recipient's |
| ContractAccessGrant | none | The frozen agreement between two parties. "A show manager cannot widen another agreement's disclosure policy. P2 records the principals and consents required for such a widening" |
| InvoiceAccessGrant | none | Financial, for the same reason Finance is not in the concert's set |
| ThreadAccessGrant | none | `ThreadEntity.AddParticipant` already owns admitting a tenant to a conversation; a second path would be a second policy |

A family with no shareable scope gets no share route: nothing in P1 can widen it, and P2 owns the
consented widening.

A read grant grants no signing, approval, debit or settlement authority. Mutations also validate the
resource's owner or accepted operation assignment, expected version and allowed lifecycle state.
Insert/update/delete and raw/bulk paths must use that same command boundary. An interceptor can enforce
required IDs, immutable content and authorised write scope; it cannot invent an ownership policy.

### Membership, profiles and entry points

Replace single TenantType authority with zero or more TenantBusinessProfile(TenantId, Kind,
ActivatedAt, RetiredAt), unique by tenant/kind, admitting VenueOperator, Artist and Promoter.
An agency or production business can have no marketplace profile. Replace type-selected permission
catalogs, RequiredTenantType endpoint checks and frontend type-as-authority branches. Profile endpoints
still validate their particular profile. Retiring a profile changes eligibility for new work, not an
existing agreement's identity or settlement rights.

Tenant persists ContactEmail separately from LegalName, initialised by tenant creation and changed
through its settings command. Creation/Announce events use ContactEmail even after legal setup.
Tenant.Contracts exposes BusinessFacts(TenantId,LegalName,ContactEmail,TaxComplianceStatus,
BusinessProfiles,AuthorityVersion) for authorised internal consumers. Existing tenant-level tax and
verification operations remain the eligibility boundary; provider readiness remains Payment-owned.
Replace ITenantContactResolver's profile dispatch in verification/admin/notification consumers with
these Tenant-owned facts, including batched list queries.
Do not resolve a promoter's legal/contact facts through Artist/Venue or equate a profile with payment
verification. Onboarding creates the legal tenant/membership first and activates only chosen profiles.
P1 replaces the type-dependent provisioning, identity response, invitation/setup forms and synthetic
tenant seeds together; no fake Venue/Artist row is needed to provision a business or receive a grant.

Use a role-only permission catalog plus operation-specific resource/eligibility policy:

| Role | Commercial permissions in the replacement |
|---|---|
| Owner | Sign own principals, manage own resources/sharing/representation, finance and membership administration |
| Manager | Sign own principals and manage assigned operations; see authorised terms/finance; no mandate issuance, payout-account management or role administration |
| Finance | Authorised finance reads, payer commitment and settlement execution; no agreement signing |
| Staff | Assigned operational edits/messages; no terms/finance/signing by default |
| Door / Sound | Existing limited operational functions on expressly accessible resources; no finance/signing |
| RestrictedParticipant | Assigned operational/evidence reads and P5 evidence decisions only; no broad messages, terms, finance, sharing or signing |

Carry existing noncommercial role permissions deliberately into the new catalog. Adding Promoter does
not give every role organiser powers. Replace the Membership type and local permission derivation in
app/shared, including membership resolution, useTenant and tenantSession. Server-returned permissions
and actions drive app/web/shared and the actual app/web/business, artist, venue and admin consumers.
Business hosts the neutral operational screen; artist/venue surfaces require the relevant active
profile as well as permission, not an exclusive tenant type.

Update app/mobile's chooser, switcher, active-tenant context and RootNavigator in the same P1 slice.
Every selected business can reach neutral assigned operations; permitted Artist/Venue tabs are
optional sections for the active profiles. Never fall through from a non-venue business to ArtistTabs.
Restricted members see only assigned operational actions on both platforms. Clear cached queries,
subscriptions and pending commands on tenant switch and prevent an old response repopulating the new
tenant's state. Tokens remain sub/email identity only.

Authorization owns an explicit execution scope for interactive, public, system and moderation use.
No HttpContext by itself grants bypass. Keep shared ITenantContext signatures if required by the
platform package, but IsHost must reflect an explicitly established trusted execution scope.
Workers, seed hosts and outbox handlers establish purpose and resource scope at their composition
roots. Moderation additionally requires current admin authority. A user-supplied header cannot select
any privileged stance. ReadDbContext is a read capability, not permission to expose private data.

Downloads reauthorise their own facet and stream through B2B. Use private document/evidence storage;
do not issue reusable bearer URLs that outlast access. SignalR messages and queued notifications
carry only a safe invalidation/reference; resolve recipients and access again at delivery. Revoke
subscriptions and caches without claiming that already delivered bytes can be recalled.

### Indexes and revocation

Each grant family needs resource-first (ResourceId,TenantId,Facet,MemberUserId) and tenant-first
(TenantId,Facet,ResourceId,MemberUserId) indexes, filtered on RevokedAt IS NULL. Separate unique filtered
indexes handle tenant-wide and member-specific grants; do not rely on nullable uniqueness implicitly.
Expiry remains a predicate, not a moving filtered-index condition. Add source-authority indexes in P3.

Membership changes/removal and resource grant mutations share a transaction fence with protected
writes. Lock/check authoritative tenant membership/permission revisions and resource grant versions
before commit. A read followed by an unchecked save does not provide revocation safety.
Reads linearise at their authorisation query; writes at their fenced transaction commit.

P1 owns Membership.AuthorizationVersion, Tenant.AuthorityVersion and the narrow authority-facts
relation used by reads. All membership/role/profile-eligibility mutations update the appropriate
version. P3 extends this relation with signing-grant status; it does not create the first fence.

P1 also introduces the B2B DataAccess transaction coordinator. Tenant, the current resource modules
and outbox enlist in one connection and DbTransaction; nested units of work join and cannot commit
independently. Integrate current protected writes, including the existing acceptance/Booking path,
with that fence before P1 is qualified. Allocate/reuse operation identity before retries, rerun the
whole database action in a fresh scope, and dispatch external IO after durable intent commits.
P2 extends the same coordinator to Show and replacement agreement content. Sharing both objects is
required by [EF's cross-context transaction contract](https://learn.microsoft.com/en-us/ef/core/saving/transactions#cross-context-transaction);
the current independent context registrations must change in P1. Prove rollback and both revocation
orders with real SQL Server connections, not only mocks of the coordinator.

## 5. Show, proposals and accepted agreement schema

P2 replaces fixed accepted identities and application-specific downstream contracts. New identities
are non-empty UUIDs; existing resource int IDs stay int because changing their key type adds no
capability. This is the new schema, not a mapping from old records.

| Owner/table or value | Stored shape and constraints |
|---|---|
| Tenant, supplied by P1 | Membership AuthorizationVersion, tenant AuthorityVersion and read-only policy facts; P2 consumes these existing fences |
| Show / ShowEntity | Id, ManagingTenantId, title, timezone, current ScheduleRevisionId, bigint Version; management is coordination only |
| Show / ScheduleRevision | Id, ShowId FK, proposed period and immutable schedule facts; agreements pin a revision, never silently inherit a later edit |
| Show / ShowParticipant | Id, ShowId FK, TenantId, invitation state, Version; unique ShowId/TenantId; finite role children allow multiple descriptive roles |
| Show / BookingSlot | Id, ShowId FK, requested interval and Version; one slot is one independently bookable engagement opportunity |
| Show / VenueUseAuthorization | Id, ShowId/SlotId FKs, selected VenueId, operator TenantId, schedule revision, issuer actor, status and Version; exact slot/location permission from the venue operator |
| Show / SlotClaim | SlotId FK, EngagementId, AcceptanceOperationId, state; unique active SlotId, unique acceptance operation; no claim at mere proposal creation |
| Show / PerformerClaim | Id, SlotClaim FK, ArtistId, interval, state; unique active acceptance operation; ArtistId lock-owner row serialises overlapping-interval checks across shows |
| Show / EngagementLink | ShowId FK, EngagementId, typed entry origin and observed Booking/Concert IDs/status, deduplication/version; grouping projection only |
| OpportunityEntity | Managing TenantId, immutable ShowId/SlotId, selected slot/schedule versions, offered content/policy version and edit token; no Show/Slot retarget after creation |
| Entry root | ShowId, SlotId, EngagementId, AgreementId allocated once; current RevisionId and bigint edit token; entry state and owning/proposing tenant |
| Entry / AgreementParticipant | ParticipantId, AgreementId, immutable TenantId; unique agreement/tenant identity; no retargeting an existing ID to another business |
| Entry / ProposalRevision | Id, AgreementId, revision number, previous revision, schema/semantic versions, canonical content hash, rendered text and seal; unique agreement/revision number |
| Entry / RevisionParticipant | RevisionId FK, ParticipantId FK within AgreementId, capacity and agreed legal identity; representative capacity added in P3 |
| Entry / SettlementBinding | RevisionId FK plus payer/payee ParticipantIds; composite FKs require both to belong to that exact revision and be principals |
| Entry / Consent | Id, RevisionId/hash, principal ParticipantId, actor UserId/TenantId, signature/evidence/time, membership/permission/policy version and request ID; append-only evidence outside the proposed-content seal |
| Booking / ContractEntity | BookingId unique FK, AgreementId, accepted revision head; single sealed accepted aggregate replaces subtype-specific contract identity |
| Booking / ContractRevision | Id, ContractId FK, source proposal RevisionId, predecessor if later supported, AgreementSnapshot, content/acceptance hashes and seal; accepted child participants/bindings/consents have real FKs |
| BookingEntity | AcceptanceOperationId unique, EngagementId unique, ShowId/SlotId/AgreementId, ContractRevisionId FK, typed origin, confirmation state and immutable payment references |
| ConcertEntity | BookingId unique, same lineage/origin/ContractRevisionId, immutable snapshot copy, separate operational and execution facts; remove pair financial columns |
| Concert / InvoiceEntity | ContractRevisionId provenance, accepted payer/payee tenant identity resolved into Supplier/Customer InvoiceParty, amounts and sequence; private grants independent of concert summary |
| Show / NativePublicationClaim | ShowId unique, EngagementId, operation/version. At most one engagement supplies native ticket inventory for a show in this foundation |

ShowAccessGrant(Summary/ManageSlots/ManageParticipation), ProposalRevisionAccessGrant(Terms), and P5
RequirementAccessGrant(EvidenceRead/Submit/Decide) follow section 4's typed FK/facet/member restrictions
and indexes in their owning modules. Show membership is descriptive participation, never a replacement
for those grants. Entry consent has composite FKs to its revision/principal, unique request identity,
and an index by revision/principal/time. A new valid consent may supersede invalid authority evidence
without editing the old row; final acceptance deterministically selects the latest qualifying consent
for each required principal under the entry fence.

A BookingOrigin is a closed ApplicationOrigin(ApplicationId,OpportunityId) or
DirectInvitationOrigin(InvitationId) union. Relational storage uses a discriminator with checked
alternative columns and indexes for each branch. Delete the unconditional ApplicationId/OpportunityId
requirements and application-only uniqueness/correlation. An invitation never supplies fake IDs.

Show creation/participation and venue eligibility are explicit operations. A promoter selects an
existing venue profile for location; its current operator issues VenueUseAuthorization for the exact
slot and schedule through a command validating Venue ownership and resource-management permission.
That authorisation grants use of the selected resource, not access to every artist agreement.
Revocation blocks new acceptance; it cannot erase an already accepted use. Changing accepted use
requires the owning cancellation or later amendment capability.
The current Venue profile can serve as a resource reference while the premises/rooms owner designs
its broader inventory. Do not claim that a ShowParticipant role proves a room reservation.

Opportunity authoring selects an existing Show/Slot under current management permission; it records
that mapping once. Publishing and accepting require a current venue-use authorization and eligible
slot/schedule. Application preparation obtains these authoritative references from Opportunity;
the applicant cannot supply or replace them. Changing the offered content increments the opportunity
policy version and requires prepared applications to produce a fresh revision/consent. Withdrawal,
Filled state or a changed pinned slot/schedule rejects final acceptance. Moving recruitment to another
slot means a new Opportunity. DirectInvitation selects the same Show-owned slot contract in P4.

One Show can hold headliner/support slots with independent engagements and agreements. Neither a
shared schedule nor a claim on one slot confirms/cancels another booking. Existing performer collision
checks must become authoritative interval claims, locked by ArtistId before acceptance, so two slots
in different shows cannot concurrently double-book the same performer. The existing day/date
availability projection alone is not a concurrency constraint.

A failed confirmation retains the slot/performer claim. Release only after the owning Booking/Concert
cancellation and required financial resolution commit. An unknown provider outcome is not a release
condition. Show receives durable facts and updates its grouping projection; it does not poll child
aggregates to infer permission or money state.

### AgreementSnapshot and finite attachment contract

AgreementSnapshot contains:

- AgreementId, proposal RevisionId, ShowId, SlotId, EngagementId, pinned schedule and typed origin.
- PerformanceAttachment: typed performer and location references plus pinned scheduling/public facts,
  as specified below; Application origin also pins its Opportunity content/policy version.
- Schema version and economic/capability semantic versions; the effective immutable DealTerms.
- All RevisionParticipants and their agreed legal identities/capacities.
- SettlementBinding and the separately selected TicketSellerParticipantId when native publication is used.
- Required-consent set, exact rendered terms, proposed-content hash and disclosure policy.
- Collected consent/authority evidence, acceptance operation and legal/payment mandate versions.
- Payment commitment references and destination ownership constraints.
- From P5, the selected requirement definitions and any pinned pre-acceptance evidence.

PerformanceAttachment is proposed content: PerformerReference(ArtistId,PerformerTenantId) and
LocationReference(VenueId,VenueOperatorTenantId,VenueUseAuthorizationId,AuthorizationVersion), with
ScheduleRevisionId, SlotVersion, StartUtc/EndUtc, timezone, agreed genres and location display/address/
coordinate snapshot. The current finite performance capability requires one selected artist and venue;
that does not constrain the number of agreement participants. Validate profile ownership and current
slot/resource eligibility through Artist, Venue and Show contracts before consent and again at acceptance.
The selected performer supplies the ArtistId interval fence. Neither selection comes from payer/payee.

Copy this attachment unchanged into AgreementSnapshot and ConfirmedBooking; Concert creates its
execution facts from that handoff without reopening an entry. Public display-only artist/venue names,
images and descriptions may refresh through bounded profile projections. Financial entitlement,
profile identity/ownership, period and agreed location cannot change through such a refresh.
This foundation exposes no command to silently move an accepted engagement or substitute a performer.

The proposal hash covers canonical proposed content and the required-consent policy. Signatures attest
that hash. A separate accepted-envelope hash includes the resulting signatures and authority evidence;
a signer cannot sign a payload containing their own not-yet-created signature. Specify schema-versioned
serialization: ordinal identifier ordering, UTC timestamps, invariant decimals/currency, explicit
nullable fields and bounded collections/strings. Set order must not change the hash.

A ParticipantSlotDefinition carries stable slot identity, finite semantic kind, permitted capacity,
cardinality and required capability/input facets. P2 provides Payer and Payee slots for the direct
settlement capability; P5 adds Respondent and Approver for its real requirement. ParticipantBinding
resolves a declared slot to a participant in the exact revision. Validation returns typed failures for
missing/duplicate bindings, foreign revision, ineligible principal, unsupported cardinality/capability
or missing minimum input visibility. A binding never manufactures its own access grant.

New supported configurations may use these contracts without new participant columns or a new
whole-Deal type. The configuration owner supplies its economic graph and finite capability declarations;
it must not replace identities with role strings, JSON tenant-ID bags or live profile lookups.

## 6. Consumption contracts, money and atomic acceptance

All HTTP actor identities derive from current validated membership. Client UUIDs, represented
participants and expected versions are selectors. Application services return operation-owned
Result/error unions; only API endpoints map them to status codes. Resource hidden → 404; visible
resource but forbidden action → 403; stale version/reused key with different content → 409.
Lists are bounded/keyset-paged. Downloads return bytes/content type/name after facet authorization.

| Producer/operation | Exact input and consumed output | Boundary |
|---|---|---|
| Tenant profile/membership read, P1 | Active validated membership → TenantId, member role/version, BusinessProfiles, Permissions | Synchronous HTTP JSON for switcher/navigation; replace type-driven DTOs in the same slice |
| Resource share/revoke, P1 | ResourceId, recipient TenantId/optional member, allowed facet, validity, expected version, request ID → GrantId/Version/status | Owning module command, HTTP inline; server enforces its disclosure policy |
| Concert operational read, P1 | ConcertId → name, period, location/artist public labels, state, permitted operational actions | Separate response with no fee, payment, invoice ID, contract/blob reference or unrelated participant list |
| Show create/slot/participation, P2 | Versioned title/schedule/resource request or addressed invitation → ShowReference(Id,Version,ScheduleRevisionId), SlotReference(Id,ShowId,Version), participation status | Synchronous module contracts and HTTP commands; notifications through outbox |
| Opportunity author/publish, P2 | ShowId/SlotId, expected Show/Slot/schedule versions, offered terms and request ID → OpportunityReference(Id,ShowId,SlotId,PolicyVersion,Version,State) | Opportunity owns immutable slot mapping; Show validates management, slot and venue-use permission |
| Application prepare/submit, P2 | OpportunityId/expected policy version, selected ArtistId and supported terms/participants, request ID; submit exact revision/hash and signature → Proposal(Id,Origin,ShowId,SlotId,EngagementId,AgreementId,RevisionId,Hash,Version,State,Actions) | Prepare inherits Opportunity's slot/location and allocates agreement identities before checkout; submit supplies artist consent only |
| Proposal replacement, P2/P4 | EntryId, expected head/version, changed typed content and request ID → new immutable Proposal revision for review | Entry owns write; old consents cannot satisfy the new hash. Submit/Issue or RecordConsent signs the exact returned revision after review |
| DirectInvitation prepare/issue, P4 | Prepare: ShowId/SlotId and expected versions, ArtistId, addressed principals, supported terms and request ID → Proposal; Issue: exact revision/hash, proposer signature and expected entry version → Issued proposal | Show validates the selected slot/location; issue records only proposer consent and creates no recipient consent or Booking |
| PrepareCommitment, P2 | Entry/revision, payer binding, request ID → CommitmentReference, operation ID, readiness state and Payment session/quote | Payer-authorised POST; external preparation after durable intent, no provider call inside the acceptance transaction |
| RecordConsent, P2/P4 | EntryId, exact revision/hash/edit token, principal ParticipantId, signature artifact, optional P3 signing grant/version, request ID → ConsentReceipt(ConsentId,RevisionId,Hash,remaining required principal IDs,current readiness) | Entry appends valid consent; no Booking, resource claim or payment effect; does not change proposed content/hash |
| Accept, P2/P4 | EntryId, exact revision/hash/edit token, finalising principal ParticipantId, optional final consent artifact/P3 grant version, request ID → AcceptanceReceipt(Origin,RevisionId,Hash,AcceptanceOperationId,BookingId,ConfirmationState) | A currently authorised required principal finalises; optional final consent and complete-set validation commit atomically. Missing/invalid consent returns conflict with no partial write; use RecordConsent to collect it first |
| AcceptedAgreement, P2 | AcceptanceOperationId + immutable AgreementSnapshot → BookingAcceptance(BookingId,ContractRevisionId,ConfirmationState) | Synchronous entry→Booking contract inside shared B2B transaction; idempotent by operation, engagement and hash |
| ConfirmedBooking, P2 | BookingId, ContractId/RevisionId, accepted hash, AgreementSnapshot and actual confirmation/commitment evidence | One durable BookingConfirmedEvent → Concert inbox; replace the existing internal message directly |
| Concert publication, P2 | ConcertId + accepted publication authority; positive native inventory also requires Show native-publication claim → ConcertChangedEvent public projection | Durable integration event; package closure in section 10 |
| Issue/revoke signing grant, P3 | Principal-selected agreement/show scope, ActingTenantId, validity, explicit limits/evidence, expected version → GrantId/Version/evidence hash/status | Tenant-owned HTTP/module command; principal Owner only |
| Evidence submit/decide, P5 | Requirement/assignment version and exact evidence revision/hash → EvidenceRevisionReference or ApprovalDecisionReference/status | Stage-owned HTTP JSON; attachments use separate authorised streams |

Commands with durable effects reserve a RequestId under (ActingTenantId,OperationKind,RequestId)
and canonical payload hash. Exact replay returns the recorded result after current access checks;
a different payload on the same key is a conflict. Do not return an old response body containing
fields the caller can no longer see.

Consent collection never allocates Booking or charges a payer. For three required principals, submission
can record the first, RecordConsent the second, and Accept the final signature plus acceptance; the
first two responses have no BookingId. Alternatively all three record consent before an authorised
principal calls Accept. Final acceptance revalidates every selected consent's current authority and
all required payer readiness. Lost responses replay the same recorded consent/acceptance result.
Editing the proposal invalidates qualification of old consents without deleting their historical rows.

### Durable records and outcome routing

Use module-local record families per aggregate with actual owning-resource FKs, not a shared
polymorphic ResourceId table.
P1 supplies receipt/outbound-operation persistence for its current protected commands; P2 replaces
entry financial correlation with exact agreement/revision commitments. Reuse the existing outbox/inbox
transport and the sibling's OperationClaim rather than building a second delivery framework.

| Record and owner | Persisted contract and constraints |
|---|---|
| RequestReceipt, each command-owning module, P1 | ActingTenantId, ActorUserId, OperationKind, RequestId, payload hash, owning resource FK, operation ID, state, result reference and timestamps. Unique (ActingTenantId,OperationKind,RequestId); another actor or payload on that key conflicts |
| FinancialIntent, current Application/Booking/Concert owners in P1, DirectInvitation in P4 | Id/operation claim, owning resource FK, supported operation kind, payer/payee owner IDs, exact amount/currency/request hash, PaymentOperationReference, outbox correlation, state and Version. Unique operation ID and immutable authorised request facts; P2 additionally pins ContractRevisionId or its local proposal/commitment FK |
| PaymentCommitment, each entry owner, P2/P4 | Id, local EntryId/ProposalRevisionId FKs, payer ParticipantId FK in that revision, purpose, stable operation ID, provider reference, readiness/expiry, superseded flag and Version. Unique (ProposalRevisionId,PayerParticipantId,Purpose); one entry-local commitment per exact agreed content and purpose |
| PaymentOutcome, each financial-intent owner | Provider event ID, FinancialIntentId FK, matching operation/reference, observed outcome/time and bounded evidence; unique provider event ID. Append-only evidence plus fenced update of current intent/commitment state |

An inline database command creates its receipt and Completed result in the same transaction as the
effect; rollback leaves neither. An external operation commits Pending and its immutable financial
intent/outbox first. Its dispatcher returns/reconciles the same operation until the receipt points
to the completed result. Authorise receipt reads and reconstruct the currently permitted response
from that result reference. Session secrets are obtained through the payer-authorised readiness read,
never exposed in generic receipts. Keep deduplication identity for the owning business record's life;
trimming response/evidence payloads never permits that key to execute a second financial operation.

FinancialIntent progresses PendingDispatch → AwaitingOutcome → Succeeded or Failed; timeout/ambiguous
provider response enters Reconciling, not Failed or a new operation. Dispatch can repeat the same
idempotent request. Out-of-order/duplicate outcomes cannot regress a terminal verified result; resolve
contradictions through Payment's actual operation status before applying a new state. A commitment
reports Pending/Ready/Expired/Failed/Reconciling, with provider expiry checked again at final acceptance.
Superseding a revision forbids using its commitment and records void/release work as a separately
claimed operation; immutable payment outcomes and prior accepted obligations remain intact.

CommitmentReference contains the closed owner kind Application or DirectInvitation plus CommitmentId.
The Payment client-reference string encodes only that discriminator and opaque ID; it contains no
OpportunityId, artist identity or fake application ID. B2B's router selects the owning module, which
loads its persisted commitment and validates the full operation kind/ID/reference and outcome evidence.
Parsing a reference grants no authority. Booking/Concert operations keep their own typed resource
correlation and accepted commitment reference. Replace both production/test helpers and consumers;
Payment treats the client reference as opaque and keeps its supported operation-kind constants.

### Current economic capabilities

| Preset | Accepted binding and existing gross calculation | Funding/confirmation and completion |
|---|---|---|
| FlatFee | Organiser → artist; fee | Actual payer authorizes hold; Booking captures on confirmation; Concert releases/refunds under current lifecycle |
| DoorSplit | Organiser → artist; percentage × (native ticket amount basis + declared external door revenue) | Actual payer's verified commitment; collect at settlement |
| Versus | Organiser → artist; guarantee PLUS the percentage share | Same deferred-payment readiness and settlement; never change it to greater-of |
| VenueHire | Artist → venue supplier; hire fee | Artist's setup/mandate before acceptance; Booking deposit/funding; Concert release/refund |

P2 consumes accepted payer/payee IDs throughout settlement, VAT/invoice direction, commitment preparation,
confirmation and cancellation. Current venue-direct presets select the same businesses; promoter-led
performance deals select promoter/artist explicitly. VenueHire's supplier remains the venue operator:
a promoter cannot take its place just by managing the show. Additional principals require their own
consents but do not imply extra payment legs.

Keep current rounding, VAT-status input, commission ownership and financial effects while changing
responsibility selection. Tests exercise actual amounts and effects, not an adapter parity oracle.
Native ticket revenue currently uses TicketsSold × Price; authoritative sale/refund amounts belong to
the sales-evidence debt owner. Externally declared revenue must remain labelled and non-overlapping.
Do not advertise verified whole-show/multi-channel revenue or greater-of economics from this foundation.

TicketSellerParticipantId identifies the accepted native ticket entitlement and must be a principal
with its own provider account. The current presets select the payer; no agency signing grant changes
it. One NativePublicationClaim prevents independently booked acts creating duplicated whole-show
inventory. Acquire it only when a Concert exposes positive native ticket inventory; also check it when
an update introduces or expands inventory. Zero-ticket informational listings and private engagements
can proceed independently. Current Draft Concerts already progress to settlement; posting is never a
prerequisite for that path. After sales or an unknown financial outcome, do not release/reassign the
inventory claim; changes involving live inventory allocation need the later capability.
Shared multi-agreement receipt allocation remains an explicit later capability; funding an
artist agreement never follows automatically from a ticket report or external listing.

### Shared transaction and operation identity

Extend P1's B2B transaction coordinator to the replacement acceptance path. Application/DirectInvitation
owns acceptance orchestration. Tenant, Show, entry, Booking and outbox contexts enlist in **one connection
and DbTransaction** through their infrastructure registrations. Module contracts expose operations,
not DbContexts. Nested module writes join the active transaction and cannot independently commit it.
Use a fresh scope for any execution-strategy retry and test enlistment/rollback with a real provider.

Under a deterministic lock order:

1. Lock authoritative tenant membership/permission/authority fence rows in tenant-ID order.
2. Lock referenced signing grants (P3), then Show slot/venue-use authorization and performer interval
   owner, Opportunity when present, entry head, resource grants and relevant assignment rows; all
   mutating competitors, including withdrawal and slot edits, use that order.
3. Re-read the exact revision, current memberships, eligibility, grants, consent set and content hash.
   Revalidate previously collected but not yet accepted consent authority at final acceptance.
4. Claim the operation and exclusive resources; seal the accepted snapshot, persist Booking/Contract,
   stage the owning entry transition and durable financial intent/outbox records.
5. Commit once. Return the recorded receipt. Dispatch external IO only after that commit.

If revocation commits first, the action fails. If acceptance commits first, it retains valid historical
evidence while future delegated actions fail. Expiry is checked at the commit decision time. Race tests
must force both orders with two connections, not only mutate a token in one test scope.

Consume the sibling's implemented OperationClaim and OwnsClaim/OwnsRequiredClaim mappings (section 11).
Claim() allocates once; Claim(id) accepts an already allocated identity; IsHeldBy verifies without
mutation. Entities compose one claim per operation and retain unique indexes. Do not design this
value or its EF representation again. New commitment/request IDs are allocated before external IO.
The sibling's AttemptVerdict/AttemptAsync is in-process outcome classification, not a persisted
attempt journal. Keep that boundary and add the request receipt/financial intent records actually
needed here; do not claim that consuming the sibling already supplies durable command deduplication.

Create the payer's commitment intent against AgreementId/RevisionId before any checkout call.
A changed revision gets new readiness and explicit cleanup of superseded unused holds; existing
provider operations are reconciled or voided through Payment, never reused for altered content.
Replace application/opportunity encoded references with opaque commitment IDs and a typed owning-entry
correlation. Payment's operation kind stays its existing supported constant, including verification.
Replace TryGetApplicationId and application-only verification/outcome handlers together; route both
entries by the persisted commitment reference and exact operation evidence. Booking confirmation
matches its own operation/commitment, not the existence of an ApplicationId.

Checkout is already POST in ApplicationController; replace its fresh authorization ID with durable
idempotency and expose a separate read of its recorded state. Readiness and signing are separate:
sending an invitation, storing a card,
or approving evidence cannot charge an unconsenting payer. The UI must direct readiness work to
the payer even when a different principal sends or accepts.

### Accepted immutability and document storage

Seal proposal content before any signature is collected: its content header, participants, bindings,
attachments and requirement definitions reject update, deletion and added/removed content children.
Pre-acceptance Consent is separate append-only evidence with a revision FK. RecordConsent may insert
it after the content seal under the entry/authority fence, without changing that content or hash;
existing consent evidence cannot be updated/deleted. Consent insertion stops once the entry accepts.
Final acceptance copies the selected qualifying consent set into the accepted envelope and seals the
accepted header and every accepted child, including consents, in the shared transaction.

Use database write restrictions/triggers for those distinct rules and configure EF's generated SQL
for the triggers. FK constraints alone do not enforce immutability. Test direct SQL and privileged
contexts: proposed-content mutation fails, later consent insertion succeeds, and accepted-child
insertion/deletion fails. Administrative maintenance is a separate explicit capability.

On the current SQL Server provider, persist the exact schema-versioned canonical snapshot/envelope
text in an nvarchar(max) column with ISJSON validation and stored hashes, plus relational participant,
binding and consent children for constrained identities and querying. Build both from the same value
inside the sealing transaction and validate their correspondence. These are complementary views of
the new accepted record. The Postgres owner later changes provider storage while preserving signed
bytes, hashes and constraints; no dual schema or interim economic configuration engine is added.

Store exact signed content/evidence hashes and immutable artifact references. Render from the accepted
snapshot into private contract/invoice storage, with an idempotent document record and create-only blob
write. PDF generation may follow commit; the download response exposes a truthful pending state until
ready. A retry may finish the same artifact, never regenerate it from current Deal or profile data.

Later evidence, approval results, revocations and financial outcomes live outside AgreementSnapshot
and reference its revision. This foundation has no accepted-amendment command: changing a draft is
supported; changing an accepted principal, economics or disclosure obligations needs its explicitly
designed amendment/transfer capability. Ordinary membership/profile changes never rewrite accepted data.

## 7. Signing representation

P3 introduces AuthorityGrant(Id,PrincipalTenantId,ActingTenantId,current version,RevokedAt) and immutable
AuthorityGrantVersion(agreement or show scope,SignAgreement,validity,limits,evidence hash/reference,
issuer user/tenant,policy version). Same-tenant signing needs member permission, not a self-grant.
Only the principal's Owner issues or revokes. No delegation chains or self-issued claim binds another
business. Constraints use typed supported fields; reject unknown acts, ambiguous scope and unsupported
limits.

A representative has a RevisionParticipant entry with RepresentedPrincipalParticipantId pointing to
a principal in the same revision. The signature records the acting user/tenant and that principal,
plus the exact membership, grant version, scope, constraints and evidence. It fulfils the principal's
required consent, not an invented extra consent replacing the principal.

A derived resource grant references that exact authority version. Authorization queries synchronously
join the Tenant-owned grant-status relation and require that version still current, unrevoked and
in time. Async cleanup can remove stale grant rows but is not the enforcement mechanism.
Issuing a broader new grant does not revive access derived from an older one.

Terms access is explicitly granted under the resource disclosure policy before signing; a mandate
does not silently share all Show agreements. Revoking the mandate removes future delegated access
and invalidates its unaccepted consent for a later acceptance. It does not change signatures on an
already accepted revision. Membership removal, role downgrade, grant replacement and expiry use the
same commit-fence tests.

This phase admits **signing only**. A representative cannot prepare another tenant's payer commitment,
change the payee, manage its payout account or collect its money through this grant. Financial execution
uses accepted principal bindings and current provider eligibility. A signing revocation after valid
acceptance does not itself cancel a principal's independently authorised payment obligation.

## 8. Direct Invitation and an enforced requirement

### Direct Invitation

P4 adds its own module, entity, immutable revision children, state machine, APIs and UI. The minimal
states are Draft, Issued, Accepted, Declined and Withdrawn. A changed proposal creates a revision;
issuing it carries proposer consent and invalidates outstanding consent to older content. The final
acceptance is by the remaining required principals under current authority, not a hardcoded
“venue always accepts” rule.

Organiser sends an addressed offer, artist accepts/declines, issuer withdraws while unaccepted.
A counterparty proposes changed supported terms through the same revision operation and becomes
proposer of that revision. No group acceptance, extra application approval or automatic signature.
All commands include expected version and idempotency identity; current lifecycle transitions reject
late decline/withdrawal after acceptance. The same ShowSlot arbitrates applications and invitations.

Reuse Agreement.Contracts, participant validation, the authority decision and commitment preparation
contract. Entry workflows retain their own state/loading/error contracts; share the pure acceptance
validation and transaction mechanics, not a cross-stage orchestrator or an entity-shaped common base.
Booking receives precisely the same AcceptedAgreement from either route.

The minimal organiser workspace covers venue-direct and promoter-led initiation, selecting a Show/slot,
named artist, supported terms, review/consent, payer readiness, current state and actionable failures.
A Show page lists only accessible engagement summaries. Headliner and support can use different routes
without either artist receiving the other's fee, PDF or private discussion.

### Evidence and decision with an observable consequence

P5 implements one registered **EvidenceApproval** capability in Concert, with a concrete contract:

- Definition: RequirementId, kind/semantic version, title/instructions, accepted subject revision,
  RespondentParticipantId, ApproverParticipantId, assignment policy allowing that principal to choose
  and replace its authorised reviewer member, DueAtUtc,
  evidence schema and effect **PermitPublication**.
- EvidenceRevision: Id, RequirementId FK, content hash/reference, author tenant/user, submitted time,
  predecessor and schema version; immutable after submission.
- ApprovalAssignment: Id, RequirementId FK, approver participant/member, bigint Version, revoked state.
- ApprovalDecision: Id, assignment/version, subject revision, evidence revision/hash, actor/principal
  authority evidence, Approve/Reject, time and operation ID; immutable.
- RequirementState: current evidence revision and assignment, Pending/NeedsChanges/Approved,
  concurrency version; the derivation of current satisfaction never mutates past decisions.

The offered revision may select this supported requirement before agreement; its exact definition and
participants are signed and copied to Concert. Existing presets without it continue without that
gate. The foundation exposes a minimal supported requirement form, not a general workflow builder.
DueAtUtc is a concrete deadline pinned in the signed definition, not an arbitrary predicate. When the
requirement is unsatisfied and the deadline has passed, expose Overdue alongside its review state;
publication remains blocked until approval. Late submission and explicit approval remain allowed.
An approved requirement is satisfied after its deadline too. Time alone never approves/rejects or charges.
Other requirement effects, forms, relative deadline rules and automated actions belong to future owners.

Concert.Post must check the pinned requirement and current evidence/assignment state under the same
transaction as publication and its outbox event. Upload alone and a stale approval cannot permit
publication. This gives the first party-bound step a real lifecycle consequence rather than a
decorative approval status. It does not imply that production approval authorises payment.

Production members get only their evidence/operational facets. Assignment requires membership and
input visibility; a missing grant is a validation failure, not implicit disclosure. Assignment changes
within the agreed approver principal require that principal's resource-management permission.
Changing the contractual approver principal or effect needs renewed agreement and is unsupported
after acceptance in this foundation.

Replacing evidence or reassigning the reviewer reopens current satisfaction while retaining old
decisions. A decision pins the assignment and evidence version; concurrent replacement wins or loses
under the fence. Unrelated requirements retain their own outcomes. If publication already happened,
new evidence records outstanding review/change state; it cannot undo that publication, repeat its
external effect or silently alter fees. Any already-performed consequence follows a later supported
change capability.

One Show may share a rider reference but each affected agreement has its own accepted requirement and
approval decision. Reading shared evidence does not reveal underlying artist contracts.

## 9. Implementation phases and verification

No phase below is delivered. Each phase ends with its entire exposed behaviour usable and secure,
a focused green candidate, review and applicable exact-head delivery checks. Commits may divide work
inside a phase; do not expose half a resource's security cutover. Combine adjacent phases in one PR
where dependencies permit; package publication is the real reason for a separate delivery boundary.

### P1 — replace pair access and enable restricted third-business participation

**Change:** implement section 4 across all nine entities, contexts/repositories and actual public,
private, system and moderation consumers. Add neutral membership/profile permissions, explicit system
scope, module-owned resource grants, Thread and per-member read state. Move authorization into its
own module with Tenant implementing facts; add Tenant contact/eligibility facts and zero-profile
onboarding. Implement membership versions and the shared transaction fence for current protected writes.
Replace the pair-derived financial direction in its live consumers and update the touched guidance.
Provide share/revoke and neutral operational concert access across shared, web and mobile
membership/session consumers.

**Consumed result:** the P1 contracts in section 6. A tenant with a Promoter profile and no Venue row
can read an expressly granted operational resource and nothing beyond its facet. Existing direct
principals use grants too. Broader financial participation waits for P2's signed binding.

**Concrete verification:** normal/read/privileged contexts; outsider and forged tenant/member IDs;
cached EF model with two active tenants; membership removal/downgrade races; expired/revoked grants;
summary allowed while full terms, invoice/PDF, counts, attachments, mutation and private messages deny;
reporter/admin report isolation; member watermark independence; direct-SQL/write/delete boundary;
SQL translation/no cyclic filter; single-owner profile browsing still works. Verify the UI switcher
and shared summary with distinct authenticated users in exact-head browser evidence. Qualify mobile
promoter-only, multi-profile and restricted memberships plus tenant-switch cache isolation on an
emulator/device. Browser evidence alone does not qualify native navigation. Exercise zero-profile
onboarding and tenant contact after legal-name changes.

**Schema/build gates:** regenerate affected InitialCreate/snapshots and seeds, run affected
DataAccess/Tenant/Conversations/Application/Booking/Concert builds and unit/integration gates. Grep
source/tests/registrations for the removed pair mechanism; only historical removal instructions may
remain. Do not label this done with the normal contexts fixed but read/download
routes unqualified.
Run the existing npm run build:web and npm run build:mobile scripts; the latter runs shared builds,
mobile TypeScript checking and Android Expo export. Record the native journey evidence separately.

### P2 — explicit accepted participants and promoter-led application bookings

**Change:** implement sections 5–6 in the existing application route: Show/slots and authoritative
Opportunity slot mapping, pinned performer/location attachments, performer claims, immutable
proposals/accepted Contract, intermediate consent collection, SettlementBinding, neutral accepted/confirmed
handoffs, extension of P1's transaction, payer-specific checkout and durable outcome correlation. Consume the
sibling attempt API and close the composed-claim/unstable-checkout-ID debt here. Replace fixed financial
tenant fields and the profile-derived ticket user. Adapt current preset forms, documents, seeds and
public event producer/consumers; choose no new economic formula.

**Consumed result:** Application → AcceptedAgreement → Booking → ConfirmedBooking → Concert, carrying
the same Show/Engagement/Agreement/revision/binding. A promoter books at a separately operated venue
without acting as that venue. Venue sees its authorised operational scope; performer sees its own terms.
The external publication contract closes through Customer and Search under section 10.

**Concrete verification:** all four formulas and actual payer/payee/invoice/ticket direction; independent
venue/promoter/artist identities; invalid/foreign/missing principal bindings; hash determinism and
changed content requiring fresh consent; contract creation only from acceptance; accepted child
immutability via EF and SQL while later consent can append to sealed proposed content; three-principal
consent across separate requests with no early Booking; exact slot inheritance and Opportunity
withdrawal/revision races; fault injection between entry, Show claim, Booking/Contract and outbox;
concurrent same-slot and same-performer acceptance; duplicate success/failure/confirmation and lost
HTTP response; changed-revision checkout and release/reconcile of unused holds; payment outcomes
match the right commitment without ApplicationId routing. A confirmation failure creates no Concert.
Native publication claim prevents duplicated show ticket inventory.
Zero-ticket/private engagements still progress independently, including Draft-to-settlement; refreshing
public profile copy cannot change the accepted performer, location, schedule or financial identities.

**Gate:** exact source/attempt dependency resolved, current-provider transaction/integration tests,
producer package publication, both consumer bumps and clean published-baseline revalidation. A local
pack alone does not finish this phase. Preserve current payment/commission policy; route any changed
Payment quote surface through its actual owner before exposing a new total.

### P3 — delegated signatures with immediate revocation

**Change:** implement section 7, derived access validity, grant management and representative-aware
proposal/signature screens. Include actor/principal evidence in immutable accepted snapshots.

**Consumed result:** agency signs for an artist under one exact scoped grant; artist remains payer/payee
as agreed. The agency can read only explicitly granted terms while its source authority is valid.

**Concrete verification:** self-sign and representative sign; wrong principal/scope/version/act/limits;
no transitive delegation; grant expiry/revocation, role change and membership removal racing consent
and final acceptance; a previously collected unaccepted consent ceases to qualify; an accepted signature
remains historically valid; derived downloads deny after revocation; payout destination and payer
commitment unchanged. Exact-head browser proof displays actor and represented principal distinctly.

**Gate:** no signing endpoint ships before authoritative validity and two-connection fence tests pass.
No receipt/collection API or unsupported authority placeholder is added.

### P4 — direct invitation through the same accepted contract

**Change:** implement DirectInvitation from section 8, both entry policies and the organiser/artist
UI flows. Reuse the P2/P3 contracts and financial preparations with route-specific orchestration.
Replace remaining assumptions in availability, action links, notifications, payment verification,
seeding and dashboards that every Booking has an Application/Opportunity.

**Consumed result:** directly invited headliner and recruited support share ShowId, have distinct
EngagementIds and private agreements, and reach the same Booking/Concert APIs. Promoter or venue
can propose; counteroffers follow exact-revision consent.

**Concrete verification:** eight route × preset integration journeys; venue-direct and promoter-led
UI journeys for fixed fee and revenue share; direct VenueHire payer readiness; send/accept/decline/
withdraw and counteroffer retries; competing application/invitation acceptance with one slot winner;
wrong addressed tenant, stale hashes, representation revocation and no recipient consent on send. Assert zero
Opportunity/Application rows are created by the direct route and no fake IDs enter provider correlation.

**Gate:** one route-independent accepted contract, explicit source-specific errors/actions and truthful
funding states. No extra financial capability is smuggled in to make a direct route pass.

### P5 — enforced evidence approval and foundation closeout

**Change:** implement the Concert requirement in section 8 and its minimal definition/submission/review
UI, capability binding validation and publication gate. Close the security, documentation and
configuration-handoff obligations below.

**Consumed result:** a production member reviews only assigned rider evidence; current approval permits
publication; replacing the evidence makes the required approval outstanding without altering fees or
executed actions. Deal Configuration receives implemented participant/binding/requirement contracts.

**Concrete verification:** complete the selected requirement through both entry routes; changed
evidence, reassignment and stale decisions; publication-versus-replacement race; rejected/missing/late
evidence; production member denied sign/finance/other agreements; payment unchanged; two independent
requirements do not invalidate each other; repeated decision/publication produces one result/event.

**Final qualification:** the entire matrix in section 13, measured access SQL/index behaviour, scoped
downloads/notifications, both real package consumers and all changed guidance/debt entries. Publish
the actual consumed contracts and delivery evidence to the dependency owners. Close this plan only
after all producer/consumer merges and required generated syncs are terminal.

### Verification execution, including actual CI coverage

Local default: required generators, grep/invariant checks, smallest affected builds and focused unit
tests. The reported 533 passes are a prior baseline, not a target count or proof of this redesign.
Docker-dependent tiers require actual execution evidence before delivery.

Current .github/workflows/ci.yml runs Unit, Integration, Architecture and Startup, validates migrations
and packages, and builds artifacts. It does **not** run the Api/Ui E2E categories. Run
.github/workflows/e2e.yml at the exact candidate commit for affected lifecycle/process/browser proof;
record that head and digest-pinned dependent services. Do not describe ordinary PR CI as full browser
coverage. Follow the existing platform fixture rather than restoring monorepo source references.
If required credentials/runner capability is unavailable, that tier stays an explicit delivery gate.

Use the real provider for transaction, indexes, uniqueness, triggers, expiry and concurrency tests.
Unit tests for pure binding/hash/transition behaviour supplement those checks, not imitate EF.
Existing meaningful UI tests should cover the changed journeys; new scenario coverage belongs in the
authorised API/browser tiers, not a duplicate frontend unit-test architecture.

## 10. Published contract closure

Only the changed **Concert publication** surface has verified behavioural external consumers in this
scope. The wider package graph still matters:

| Consumer evidence | Required closure |
|---|---|
| Customer cached main ce2b8cc5cdc50af9d0b35eeb82c431496ffaf625; inspected primary checkout 3871c656 | Directory.Packages.props pins ConcertableB2BContractsVersion 0.1.0-alpha.0.1381. Concert projection stores both payee IDs; TicketService uses PayeeOwnerId for Payment |
| Search cached main cc7d1946368cb9bc622254602b1ece434bbac314; inspected primary checkout d0e29e7 | Same B2B package version. Concert projector consumes public listing facts, not financial recipient IDs |
| Customer/Search also reference Artist.Contracts, Venue.Contracts, Seed.Contracts and Hosting | Keep genuine profile publication facts; rebuild/update seed constructors and published topology/hosting closure when affected. Those references are not reasons for legacy readers |
| Search AppHost references Tenant.Contracts | Its actual use is PayoutOwnerRegisteredEvent(OwnerId,Email), which has no TenantType/profile field. Changing private membership policy does not require changing this event |

Consumer implementation paths on their cached-main layouts are under api/src; Directory.Packages.props
is at each root. The inspected older primary layouts use src; rename-aware comparison found the
relevant consumer implementations and package pins unchanged. Recheck each active owner before edits.

Replace ConcertChangedEvent's payload once: retain approved listing fields and ConcertId, add
ShowId/EngagementId for grouping, replace profile-derived PayeeUserId/PayeeOwnerId with
TicketSellerTenantId. Remove the old event signature and unused ticket-user resolver. Preserve
Payment's own PayeeOwnerId term at the Customer→Payment adapter; it maps from the new seller tenant.
There is no runtime consumer of the old PayeeUserId beyond storage/projection in the inspected
Customer source. Remove that field there and in ConcertSeedSpec/test fixtures.

Use one event and one active schema/message identity, updated consistently with all registrations.
No parallel V1/V2 event classes or fallback deserializers. Add no private participant, signature,
grant, fee or evidence data to the public payload. Search need only persist grouping fields it uses;
both consumers must compile and deserialize the complete new event, including through B2B seed mappers.

Delivery chain for P2:

1. Freeze the B2B producer candidate, pack reproducibly and record commit, package version and hashes.
2. Prepare Customer and Search branches against that exact local artifact, including seeds/hosting.
   Temporary feed paths and versions never enter committed config.
3. Merge/publish the B2B producer and follow its generated package/version sync to completion.
4. Bump both consumers' central B2B version property to the published version, restore from the real
   feed, remove temporary inputs and run their affected tests plus required CI.
5. Merge both consumers and required generated syncs. Re-run the integration/system proof with their
   exact published artifacts before claiming promoter native publication ready.

These are back-to-back delivery legs of this foundation, not optional cleanup. No deployment is
started with incompatible mixed artifacts; there is no live service requiring an expand/contract
rollout. Provider package changes discovered in implementation get their own named producer/consumer
edge, not a guessed compatibility workaround.

## 11. Dependencies and debt before Deal Configuration

### Existing owners and merge order

Recheck branch heads, dirty paths and PR heads before implementing; never write in a sibling checkout.

| Owner | Observed state | Implementation versus delivery |
|---|---|---|
| Refactor/DealVocabularyAndMapperCollapse | Local acd4a6ed69559e03d8fd1458d6edb55fce3d822c; dirty TECH_DEBT.md; no branch PR observed | 7d0e3024 supplies OperationClaim/owned mappings; 34f5042b supplies in-process AttemptVerdict/AttemptAsync; 8d17865a removes the unused settlement resolver family. Consume these exact commits and reconcile overlapping files before P1/P2 delivery; do not recreate their work or trust the stale ledger's claim that implementation is absent |
| Feature/MonorepoBacklogPort | Local d2613b7d7d3a7e88ea05747f9a42c20beffe7126; PR #14 remote c4e1e88e3d2c181410c4b695e09deb141465f8aa; eleven generated UI test files dirty | Reconcile HasLiveObligationsAsync, human erasure and ActionLink changes against P1/P2. Its erasure does not retire tenants/accounts; its shared ActionLink still interpolates routes. “Recovered/approved” commercial design is not runtime implementation authority or proof it landed |
| Fix/CiCompleteCheckContext | Local/PR #15 c5993e497939cbb8e71186a725703a541164f031 | Required CI-context repair; verify landed before relying on the merge gate. Does not block local design/implementation |
| Central Deal Configuration | Economic language/configuration owner | Pure language/evaluator design can consume this attachment contract now. Runtime participants require delivered P1–P2; representative use P3; both routes P4; evidence-bound step P5 |
| Central PostgreSQL migration | Shared preparation exists; B2B provider swap separate | Foundation relational work uses current SQL Server. If the provider lands first, regenerate/test on it; do not maintain two production-provider implementations |
| Payment commission/quote/recovery and Customer sales evidence | Existing service owners | No copied commission arithmetic or assumed receipt authority. Changed published quote/fact contracts follow their own exact-artifact and publish/bump gates |

The recovered commercial plan's AgreementSnapshot name and revision/typed-origin decisions are
adopted here. Its broader ShowSpace/Room graph and relational term/step representation are not a
second implementation plan. Deal Configuration owns reconciliation with D29/D30 before its
configuration persistence: the selected hybrid relational metadata plus bounded jsonb graph remains
the target. Do not build an interim SQL Server economic graph.

Local implementation graph: P1 → P2 → P3/P4 → P5. P3 and P4 can be prepared independently against an
exact P2 artifact; the delivered representative invitation proof needs both. P1 access does not wait
for Postgres, economic language or a documentation import. P2 changes overlapping sibling source
only after integrating its exact owner candidate or explicitly superseding it with an agreed disposition.

Delivery graph: P1 reviewed/qualified → P2 producer publication → Customer/Search publication/bump
closure → P3/P4 delivery → P5 qualification and configuration handoff. Each consumer can prepare
before its producer merges; it cannot be merge-ready until tested on the real published baseline.

### Debt audit and required dispositions

The entire root TECH_DEBT.md was surveyed, including LOW and RESOLVED headings. The table below is
the planning disposition; TECH_DEBT.md remains the owner of each issue until its implementation or
source-accuracy correction lands. Imported issue headings and old links are not proof of a current
blocker. Do not leave a discovered issue without that owning entry.

Paths abbreviated as Module.Layer below are under api/src/Modules/Module/Concertable.B2B.Module.Layer.

| # / existing debt | Source status | Disposition and objective exit |
|---|---|---|
| 1. Imported guidance/paths | Live: ARCHITECTURE.md has .NET 9 and old processors; CODE_PATTERNS.md links api/TECH_DEBT.md; local route/reachability tooling absent | P1–P5 repair touched rosters; finish source-accuracy/link classification before configuration runtime. Standards owner supplies installed-plugin tooling; never copy another validator into B2B |
| 2. Operation claims | Live here, implemented in sibling 7d0e3024/34f5042b | Consume the existing claim/mapping/classification in P1/P2; close after all five operations and new request receipts have actual replay/conflict evidence |
| 3. Authorization placement | Live: Tenant.Infrastructure/Authorization and shared Infrastructure/Authorization/ManagerClients.cs | P1 owns relocation and dependency inversion; no other module imports Tenant solely for permission policy |
| 4. Workers in-memory transport | Live: Workers/ServiceCollectionExtensions.cs; Web owns real subscriptions | Host transport owner retains general relocation. Foundation handlers must use the qualified durable Web/outbox path, or complete the explicit Workers ASB/topology move before assigning them there |
| 5. Dashboard revenue source | Live B2B query in VenueDashboardService; fixture mock returns zero. External recorder diagnosis not re-proved | Sales-evidence owner replaces Payment reporting with authoritative B2B facts and nonzero assertions. Gate any advertised promoter revenue dashboard on that work; operational summary does not claim revenue |
| 6. Throwaway checkout ID | Live AuthorizeAsync(Guid.CreateVersion7()); endpoint is already POST | P2 fixes durable commitment identity/replay; correct the debt's GET claim |
| 7. Missing ConcertSalesProjection | Gross/refund projection absent; TicketPurchasedEvent and TicketSaleProcessor already exist with inbox/count updates | Customer/B2B sales owner defines recorded gross/refund basis; remove the stale event-existence blocker. Required before verified sales/shared-show settlement, not participant access |
| 8. E2E source-reference split | Old premise resolved: AppFixture uses pinned external images/packages; suite remains in B2B | Correct the entry in the guidance pass. Keep actual per-PR versus scheduled/manual tiers explicit; central system relocation remains its own owner |
| 9. Duplicate seed-host DI | Resolved in source: B2BTestClient seeding and E2E Web AddB2BWebHost | Remove stale item after focused composition evidence; do not create another seed host |
| 10. Dashboard query on write repository | Resolved by OpportunityReadRepository and module-facade composition | Remove stale item after confirming current callers; P2 Show dashboard uses proper read projections |
| 11. Tenant hard-delete teardown | Live TenantService.DeleteAsync; no TenantDeletedEvent. Human erasure sibling does not fix it | P1 rejects destructive tenant deletion with active memberships/grants/owned resources beyond its removable draft scope. Tenant-retirement owner supplies cross-module/account teardown and retention-aware final deletion; do not cascade accepted records or leave usable dangling authority |
| 12. Duplicate Venue/Artist surface | Real separate profile queries; contact moved to read repositories; keyed contact resolver exists | P1 replaces type-based cross-profile authority assumptions. Keep module-owned reads; promote a capability seam only for a concrete consumer, with Promoter legal/contact data from Tenant rather than an invented Venue |
| 13. Seed TicketsSold/Payment simulator | Existing resolution confirmed in Seed.Infrastructure/Factories/ConcertFactory.cs | Keep deterministic synthetic sale facts; no Payment seed dependency to restore |
| 14. Handwritten action URLs | Live Api mappers; sibling only shares ActionLink record. Prefix handling lives in published web package | P2/P4 changed route links use route generation and round-trip execution assertions. Broader route/web package cleanup retains its owner; update stale paths |
| 15. Admin contact N+1 | Live VerificationService loops over keyed contactResolver | P1 profile/eligibility reads must not add dual-facade or per-participant loops. Admin queue batching remains independent debt with a bounded-query-count exit |
| 16. Nullable application actions | Live ApplicationResponse<TActions> and role-specific nullable links | P2/P4 replace fixed venue/artist action responses with operation-owned, state-specific permitted actions; server enforces every action regardless of UI |
| 17. Contract PDFs in images/check-then-upload | Live Booking ContractPdfRenderer + Blob ContainerName images | P2 private document storage, immutable snapshot rendering and create-only idempotent writes; verify concurrent render and facet download denial |
| 18. Contract minting only by convention | Private setters/internal factories already exist; MintContract remains unguarded assignment | P2 acceptance-only aggregate factory/seal and database immutability close residual issue; remove false public-mutation claims |
| 19. Decimal fees | Live DealTerms/domain values converted to Money.Gbp at boundary | Deal Configuration owns typed currency semantics; preserve actual current amount/rounding here, never mix currencies through a binding |
| 20. Format-only tax verification | Live UkTaxComplianceRules regex; old class names stale | Tax-evidence owner retains authoritative verification and live-release decision; foundation captures provenance without asserting verified status |
| 21. Portal URLs | Base config supplies localhost; no checked-in production override, runtime override unverified | P1/P4 new invitation links use configured origins. Operations owner must provide/test actual environment values before customer invitation release; correct the false always-empty-dictionary diagnosis |
| 22. Thin Admin/no UI | Bare admin profile remains; moderation SPA, roster and provisioning already exist | P1 retains separate privileged moderation authority and tests report isolation. Correct absent-UI claims; richer admin roles/caching remain admin owner |
| 23. No Thread/per-thread read/retention | Thread/read structure absent; read pointers are tenant-pair based | P1 supplies Thread, grants, pagination and member watermarks. Retention periods remain policy-owned; no arbitrary purge is inferred from schema replacement |
| 24. Message-only report | Live MessageId/report pair; no second target demanded | P1 removes pair access and protects report facets, retains typed MessageId. New content targets wait for a concrete feature and its module-owned report contract |
| 25. Satellite repositories | MessageRepository handles ThreadReadStates; ConcertImages also lacks explicit owned-child exception | P1 gives ThreadReadState its own narrow repository for independent watermark commands; guidance pass declares the real Concert image child boundary instead of leaving an absolute contradictory rule |
| 26. Web payment-method callback | Published @concertable/web 0.1.0-alpha.0.6627 still exposes the ID; B2B ignores it | Web producer's own publish/bump cleanup; new P2/P4 callers post signatures/references only. Do not add raw payment-method IDs to B2B contracts |
| 27. E2E output length | Tests still use bin; e2e.yml hardcodes Playwright path. Old measured root/local-platform.ps1 stale | Repair actual output discovery if affected worktree/build exceeds limits; retain infrastructure owner and remeasure before asserting numbers |
| 28. TestEntity seed handles | Live published TestKit/SeedState.cs | P2/P4 new handles use concrete types; consolidate touched existing handles with the B2B package closure, not another generic placeholder |
| 29. Checkout base amount versus payer total | Live response gap; old charge examples not reverified | Payment pricing owner supplies authoritative quote fields; P2/P4 label agreed fee separately and never calculate platform fees. No claim of fully disclosed live charging until that producer/consumer gate closes |
| 30. Unfixed image-package exception | Live scripts/VulnerabilityGate.ps1, narrowly scoped | Image owner retains evidence/current upstream check and removes exception when fixed; no foundation-specific suppression or stale vulnerability-count claim |
| 31. PublishContainer remote default | Live Directory.Build.props ContainerRegistry ghcr.io | Build/release owner makes default local and CI destination explicit; all foundation validation uses archive output, never a publish side effect |
| 32. Historical client-key disclosure | Current env files blank; rotation/restriction not evidenced | External console owner closes recorded disclosure decision before public release; do not print keys or treat blank current files as rotation |

Before configuration runtime integration, P1/P2 must close access/authorization placement, unsafe
identity inference, operation-claim inconsistency, unstable checkout identity, accepted immutability
and the guidance that routes those changes. Thread access/read-state structure closes in P1.
Foundation phases repair stale debt claims and touched architectural rosters as they verify them.
The broader guidance pass must classify every current/target/historical claim and relocate old
monorepo links to actual owners; tooling stays in agent-standards, not a copied local validator.

Sales evidence, fee disclosure, transport qualification and tenant retirement remain explicit
capability/release gates where the changed flow consumes them. A missing remote delivery prerequisite
does not prevent pure economic-language work; it does prevent claiming that unqualified customer
journey delivered. No unrelated LOW issue becomes an invented prerequisite for participant schema.

## 12. What this enables for configurable events

| Target requirement | Foundation attachment and remaining owner |
|---|---|
| Promoter, venue, artists and contacts on a festival | P1 grants + P2 Show/participants, independent accepted agreements and slots; no tenant-column growth |
| Same business operates venue and promotes | P1 multiple profiles, one TenantId; resource/operation authority still checked separately |
| Agency signs; artist receives | P3 explicit principal/actor and authority snapshot; collection mandates and agency commission remain Payment/commercial capability work |
| Custom rider/licence/insurance request and approval | P5 typed EvidenceApproval parameters, revision-specific evidence, assignment and PermitPublication effect. New content of the supported evidence shape is data |
| Requirement at another permitted stage or with another effect | Stable participant/requirement IDs and typed outcomes already exist; that stage must implement/admit its finite capability and hand off outstanding records explicitly |
| Configured payment timing and calculations | P2 accepted binding, immutable snapshot, commitment and operation references; Deal Configuration supplies the versioned language and stage capability selections |
| Guarantee greater-of share, advances and balances | Existing Versus retains additive meaning. New operator/obligations and Payment accounting need explicit implementation; no compatibility subtype is added here |
| Multi-room/cross-booking constraints | Show/resource references and authoritative performer claims do not solve premises quotas, room reservations or annual payer thresholds; those owners must define wider evaluation facts/provenance |
| Several agreements consume whole-show receipts | Grouping identity exists; approved shared evidence may be disclosed explicitly. Sales/allocation owner must prevent double-counting and define each agreement's basis before financial use |
| Changed questionnaire/evidence after money moved | Immutable definitions and historical decisions persist. Changed evidence reopens affected outstanding review only; commercial amendment/adjustment owner preserves completed money |
| External submission or ticketing automation | Foundation tracks evidence/reference and authority. Actual integrations are deployed capabilities; neither a URL nor imported proceeds authorises a payment |

Configuration must prove two things at its own gate: a new supported combination executes without
new code/schema, and a genuinely new semantic capability is implemented once and reusable without
changing prior accepted meaning. “Any event” means unbounded supported combinations and content, with
finite enforceable semantics, not uploaded code or prose interpreted as payment instructions.

## 13. Terminal evidence and closeout

The foundation is complete only when:

- All nine pair-scoped entities and their access plumbing are replaced; third businesses and restricted
  members work through current membership, resource and facet checks across every exposed channel.
- Both entry routes, all four current financial capabilities, venue-direct/promoter-led arrangements,
  representative signatures and independently private engagements pass their consumed contracts.
- Accepted identities/terms/consents and current authority cannot race into an invalid acceptance;
  duplicate commands/outcomes cannot create a second booking, claim, Concert or financial operation.
- The P5 requirement actually gates publication and responds correctly to changed evidence and
  reassignment without disclosing fees or rewriting completed actions.
- Customer and Search consume the real published replacement event/package set, with seed/hosting
  closure, and every producer/consumer/generated-sync delivery leg is terminal.
- Current source guidance and debt dispositions agree with the implemented model; dependency owners
  receive the exact contracts, artifacts, verification and return conditions.

Measure resource lookup, tenant list, Show summary and revocation on the selected provider with actual
SQL/plans and bounded paging. Qualification datasets: ordinary 2–20 participants, a 100-participant
show, a tenant with 100,000 accessible engagements, and synthetic scale of one million engagements/
five million grants. Record hardware, dataset, index storage and p95/query counts. Initial engineering
budgets are 200 ms for a resource read, 500 ms for a 100-row list/show page and 1 s for a single
authority revocation under the recorded test load. These are test targets, not proven capacity;
an unmet budget requires query/index/locking work or an evidence-backed revised budget before closure.
Authoritative grant status makes revocation independent of synchronously rewriting every derived grant.

Use the provider owner's migration workflow to regenerate all affected InitialCreate and snapshots.
Fresh synthetic databases, application seeding and package consumers must build and run the replacement
directly. Do not retain old models or conversion tools to make the fixture generation easier.

Record final implementation, review and validation in the ledger at substantive boundaries. Keep it
while delivery remains open. At terminal delivery, update the service delivery index, move durable
rules to their owning guidance, discharge all downstream handoffs, and delete this plan and ledger.
Git history retains the design; unfinished later product capabilities retain their
own owners and completion gates.
