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
DTOs and internal messages directly; regenerate InitialCreate and synthetic fixtures. Delete every
superseded type, column, caller and event registration in its owning change. Future agreements created
by the replacement model are immutable business records; today's fixed-pair shape is deleted.

The branch contains the implemented P1 candidate. Its replacement mechanisms and accepted review repairs
are complete; current-graph qualification, final canonical review and terminal delivery remain before P1 is
landed. P2–P5 remain future work. The current execution scope and remaining delivery gates are recorded in
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

The central party document's implementation sequence is superseded by the founder's explicit
replacement requirement and this replan. Its separation of authorities and disclosure requirements
remains applicable.
The legal document's old ABSENT labels, Concert-owned acceptance and greater-of Versus wording
conflict with source. They are documentation debt, not instructions to recreate those behaviours.
No legal rates, retention periods or sufficiency of agency evidence are decided by this engineering plan.

## 2. Reviewed candidate and branch disposition

This is a source review and replacement specification, reconciled on 17 September 2026.
The frozen comparison is 309e40d4b4b704fe94246332130566b89f464de4 to
189f745d8233b511273624d768bcc39b5508723b: 16 commits, 411 changed files.
The branch is Refactor/PartyFoundationLegacyBindings and its PR is
[#18](https://github.com/Concertable/b2b/pull/18). It contains substantial P1 implementation;
the previous statement that its runtime tree equals main is obsolete.

**Keep the branch and rework P1. Do not restart it or restore the removed pair mechanism.**
The useful foundation and the defective mechanisms are separable. None of the existing test counts
qualifies the replacement below, and this source review did not run runtime tests.

| Area | Verdict | Required disposition |
|---|---|---|
| Tenant identity and neutral Authorization dependency direction | Keep | Tenant owns membership/legal/contact facts; Authorization owns request resolution and permission policy. Tokens remain identity-only |
| Zero-or-more business capabilities | Keep model, finish journey and rename | Use TenantBusinessActivity; Artist/Venue remain actual marketplace profiles. Add neutral onboarding, activation/retirement and contact settings |
| Module-local grant families and real resource FKs | Keep, repair | Exact scopes, explicit principal issuance including Contract, safe projections, membership incarnation and protected-write fences |
| ExecutionScope / ExecutionPurpose / IsHost | Replace | No ambient privilege flag. Interactive contexts always require current membership; internal processing, publication and moderation have separate capabilities |
| RestrictedParticipant and scope-free permissions | Replace | Restrictions belong to membership permission audience and resource grants. P1 repairs composition; configurable roles/hierarchy are separately owned work |
| Application/Booking shares | Remove from P1 | No demonstrated P1 disclosure contract. Keep their principal grants. P1 external sharing is Concert Summary only |
| Thread and participant-set identity | Replace | ConversationId throughout; explicit creation/idempotency, immutable initial audience, member read positions and authorized delivery |
| Settlement direction properties | Keep | Venue/Artist identify businesses; payer/payee and supplier/customer remain independent economic roles |
| One connection for every request context | Replace | One local transaction for a command; independent connections for ordinary reads and parallel dashboard calls |
| Business-context outbox insertion | Keep | The installed writer already appends to the active business context. Dispatcher storage is a separate concern |
| Server-returned permissions | Keep, complete | Same policies drive server actions; client caches and neutral routes must actually consume them |
| Existing migration/test changes | Rework | Regenerate InitialCreate after the replacement; prove actual provider/security behavior instead of counting filter strings |

### Findings owned by this re-specification

Paths below are relative to api/src/Modules unless prefixed otherwise. Every row has a P1 closure
condition in section 4 and its verification matrix; these are not accepted residual defects.

| ID | Source evidence at the reviewed head | Consequence / required closure |
|---|---|---|
| F01 | Booking.Domain/Entities/ContractEntity.cs constructor never issues its accessGrants | Both principals lose normal contract/PDF access; issue exact principal scopes atomically |
| F02 | Concert/Application/Booking DbContext filters accept any live scope; ConcertService and ApplicationMappers select private details | Summary exposes revenue/ticket counts or Deal terms; distinct exact-scope queries and DTOs |
| F03 | ApplicationService WithdrawCoreAsync/RejectCoreAsync/CancelCoreAsync; BookingWorkflow.CancelAsync | A shared-to tenant with ordinary Owner permissions can mutate another agreement, including cancellation/refund. Accept already checks venue identity; retain that check |
| F04 | ApplicationService binary ApplicationSide; ConcertService.WithActions | Outsiders become Venue and receive state-only actions; compute actions from the actual command policy |
| F05 | ConcertController invoice routes; ContractController permission | Venue profile substitutes for finance authority, excludes artist supplier and admits weak venue roles; use exact permission plus invoice/terms grant |
| F06 | TenantMembershipEntity.Create resets AuthorizationVersion to 1; resource filters omit membership Id | Remove/rejoin can revive a cached privileged request; compare membership incarnation and permission version |
| F07 | Share/revoke and lifecycle commands load then save without authority locks; MembershipService counts owners independently | Demotion/revocation races and concurrent removal of the last owners; one authoritative lock order through commit |
| F08 | ConcertEntity.Share checks IsLiveAt; grant unique indexes only filter RevokedAt | Expiry reissue/concurrent issue produces database errors; serialize issuance, retire expired row, durable request receipt |
| F09 | Share requests lack validators; ResourceAccessGrant.Revoke is public | Invalid recipients/expiry become exceptions or unusable grants; typed validation and aggregate-only mutation |
| F10 | ConcertController.GetById → ConcertReadRepository.GetDetailsByIdAsync | Published browsing can return a draft; dedicated published projection with publication predicate |
| F11 | ConcertCompletionRunner, SettlementService, InvoiceIssuer, SettlementPaymentProcessor | Workers use filtered interactive repositories; a hidden callback target can be marked processed without applying it |
| F12 | SharedConnectionExtensions; ArtistDashboardService Task.WhenAll; OpportunityDashboardService Task.WhenAll | Concurrent module reads share one connection; separate reads from the command transaction |
| F13 | Pinned platform 0.2.0-alpha.0.5, source 3136a4ee; OutboxWriter/DbContextBase/OutboxUnitOfWorkBehavior | Handoff's claim that normal outbox insertion uses the dispatcher context is false; preserve active-business-context insertion |
| F14 | ManagerClients + TenantProvisioningHandler; ActivateBusinessProfile/RetireBusinessProfile callers | No normal neutral/promoter/multi-profile onboarding or profile lifecycle command |
| F15 | TenantEntity.UpdateContactEmail has no production caller; UpdateTenantRequest | Authoritative contact is not editable; complete request, validation, settings and verification consumption |
| F16 | TenantEntityConfiguration restricted profile FK; TenantService.DeleteAsync | Existing tenant deletion now fails; deliberate activity cleanup plus live-obligation refusal |
| F17 | InvitationService role acceptance and Manager MembersInvite; MembershipService owner count | Retained Manager→Owner invitation escalation and owner-count race; explicit role assignment policy and tenant fence |
| F18 | DataAccess MembershipAuthorityFactConfiguration maps ordinary writable table | ExcludeFromMigrations is not write protection; keyless policy projection plus write-boundary enforcement |
| F19 | TenantInvitationCreatedDomainEventHandler; business/main.tsx; mobile BusinessTabs/RootNavigator | Emails target a missing acceptance route; neutral screens are placeholders and multi-profile navigation drops Artist |
| F20 | Artist/VenueDashboardController financial endpoints use OperationsView | Low-authority members can read payouts/revenue; require finance permission and bounded financial projections |
| F21 | MessageService notifies every member and copies content into ActivityRecord.Subject | Private content leaks outside message authorization; safe references and reauthorized reads |
| F22 | useTenant.ts; mobile RootNavigator; private query keys omit tenant | Invalidation preserves prior tenant data and stale responses; tenant/session keys, cancellation and switch sequencing |
| F23 | PendingVerificationDto vs admin verification/types.ts and list | Admin reads removed fields; align the actual legal/contact contract |
| F24 | ThreadRepository.GetByParticipantsAsync; MessageService create-before-transaction lookup and CounterpartTenantId | Duplicate or conflated conversations and arbitrary counterpart; explicit ConversationId and creation request identity |
| F25 | ConversationsDbContext any-scope predicate vs MessageRepository.ThreadIdsOf | Separate grants can satisfy different halves of authorization; require Read, audience and validity on the same grant |
| F26 | ThreadReadStateEntity.Advance and repository read-modify-write | Concurrent writes regress watermark or collide on first insert; serialized conditional upsert |
| F27 | ParticipantProfileProjectionHandlers keyed by TenantId | Artist/Venue overwrite business identity, neutral businesses become Unknown; Tenant-owned display projection |
| F28 | ResourceAccessGuardTests checks textual filter presence | Does not prove entity coverage, scope enforcement or write safety; provider and model-based acceptance |

The 15 September restart and discarded LegacyFinancialParties commits are history, not an instruction
to discard this candidate. Do not recover them. Current settlement direction remains in the concrete
Concert types until P2 replaces its inputs with accepted payer/payee bindings.

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
| New Authorization.Contracts / Authorization.Infrastructure | Contracts owns TenantRole, TenantPermission, IMembershipContext and the membership-fact port; Infrastructure owns request resolution, permission catalog and policy handler. Resource modules own their internal processing capabilities. Neither depends on Tenant assemblies; no aggregate or database of its own |
| B2B.DataAccess | Reusable query predicate composition, transaction enlistment and write-fence mechanics. Does not infer resource policy or load another module's aggregate |
| Each resource module | Its own typed grant tables, disclosure/assignment policy, filtered queries, scope DTOs, resource action checks and protected grant commands |
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

P1's move of role/permission/request-authority contracts out of Tenant.Contracts is retained; there are
no duplicate enums or forwarding types. Tenant business-activity kinds stay in Tenant.Contracts and
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

## 4. P1 replacement specification

P1 makes one complete journey work: a separate business, including a business without an Artist
or Venue profile, reads an explicitly shared concert summary and cannot read terms, finance, private
messages or invoke commercial actions. Existing principals retain their own properly authorized
workflows. P2–P5 remain future implementation and outside the current delivery scope.

The following before snippets describe the reviewed head. After snippets select the replacement
mechanisms and contracts; omitted ordinary mapping/error plumbing is not permission to choose another
authority, transaction or storage model.

### 4.1 Membership, permission and resource access compose once

Keep the dependency inversion: Tenant implements Authorization's narrow membership repository contract. Business activity
is eligibility for creating/operating a particular marketplace profile; it never gives a user a
permission or access to another resource. Resource grants identify accessible resources and disclosure
scopes. Membership permission identifies the human's allowed operations and whether a resource must
be assigned to that membership.

Before:

~~~csharp
public sealed record MembershipFact(
    Guid TenantId, Guid UserId, TenantRole Role, long AuthorizationVersion);

public bool IsHost => executionScope.Purpose is not null;

membership.TenantId == AccessContext.TenantId
    && membership.UserId == AccessContext.UserId
    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion
~~~

After, in Authorization.Contracts and the DataAccess request adapter:

~~~csharp
public sealed record MembershipSnapshot(
    Guid MembershipId,
    Guid TenantId,
    Guid UserId,
    TenantRole Role,
    long PermissionVersion);

public interface IMembershipReadRepository
{
    Task<MembershipSnapshot?> GetSnapshotByUserIdAndTenantIdAsync(
        Guid userId, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByUserIdAsync(
        Guid userId, CancellationToken ct = default);
}

public enum ResourceAudience
{
    None,
    AssignedResources,
    TenantResources
}

public interface IPermissionCatalog
{
    IReadOnlySet<string> For(TenantRole role);
    ResourceAudience AudienceFor(TenantRole role, string permission);
}

public interface IResourceAccessContext
{
    MembershipSnapshot? Membership { get; }
    DateTime UtcNow { get; }
    ResourceAudience AudienceFor(string permission);
}
~~~

Membership removal/rejoin creates a new MembershipId. Role changes increment PermissionVersion.
Every authorization SQL statement compares Id, TenantId, UserId and PermissionVersion with the resolved
snapshot; it must not accept a different membership whose version happens to equal 1. Request absence,
invalid tenant header, absent membership and unresolved identity all deny private access. A malformed
explicit tenant header is an error, never a fallback to a different tenant. Keep request resolution
shared through HttpContext.Items so nested command scopes do not accidentally invent a second identity.
The platform ITenantContext adapter returns IsHost = false; design-time adapters supply no authority.

The new public IMembershipReadRepository belongs to Authorization.Contracts and exposes only these two
snapshot queries. Tenant's existing Infrastructure.Repositories.MembershipRepository implements it
alongside its existing, internal Tenant.Application.Interfaces.IMembershipRepository. Keep the neutral
contract free of Tenant entities, EF types and inherited CRUD; Authorization depends only on its own
contract. Delete Services.MembershipFacts and put its queries on that existing repository:

~~~csharp
public Task<MembershipSnapshot?> GetSnapshotByUserIdAndTenantIdAsync(
    Guid userId, Guid tenantId, CancellationToken ct = default) =>
    context.Memberships
        .Where(member => member.UserId == userId && member.TenantId == tenantId)
        .Select(member => new MembershipSnapshot(
            member.Id, member.TenantId, member.UserId, member.Role, member.PermissionVersion))
        .SingleOrDefaultAsync(ct);

public async Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByUserIdAsync(
    Guid userId, CancellationToken ct = default) =>
    await context.Memberships
        .Where(member => member.UserId == userId)
        .Select(member => new MembershipSnapshot(
            member.Id, member.TenantId, member.UserId, member.Role, member.PermissionVersion))
        .ToListAsync(ct);
~~~

At Tenant's composition root, bind both interfaces to the same scoped repository instance.
Add IMembershipReadRepository to the existing repository's implemented interfaces:

~~~csharp
services.AddScoped<MembershipRepository>();
services.AddScoped<IMembershipRepository>(provider => provider.GetRequiredService<MembershipRepository>());
services.AddScoped<IMembershipReadRepository>(provider => provider.GetRequiredService<MembershipRepository>());
~~~

The Read qualifier names the narrower interface's mutability; it does not introduce another repository
implementation or a different tenancy stance. MembershipContext consumes IMembershipReadRepository
and uses the first query for an explicit
tenant and the second for the sole-membership default. Its existing resolution logic selects the active
membership; the repository only retrieves data. MembershipService retains membership administration.
No additional Lookup, Provider or service wrapper is introduced for these database queries.

P1 keeps six fixed business roles: Owner, Manager, Finance, Staff, Door, Sound. Delete
RestrictedParticipant and its seed/client cases; do not rename it into another outsider role.
Keep existing noncommercial permissions except the role-assignment repair below. Add terms.read,
bookings.cancel and concerts.declare_door_revenue as separate operation names instead of borrowing
operations.view or applications.decide. The resource permission catalog uses these explicit rules:

| Role | Permitted resource work | Audience |
|---|---|---|
| Owner | Current principal operations, terms, finance, messages, shares; tenant/member administration | TenantResources |
| Manager | Current principal operations, terms, finance reads, messages, shares; invite Staff/Door/Sound only | TenantResources |
| Finance | Summary, terms needed for finance, finance reads/settlement, message reads; no signing or commercial cancellation | TenantResources |
| Staff | Summary, operational edits/check-in, messages; no terms, finance, commercial cancellation or sharing | AssignedResources |
| Door | Summary and check-in; no messages, terms or finance | AssignedResources |
| Sound | Summary and operational edits; no messages, terms or finance | AssignedResources |

Principal policy still limits which of these operations the active tenant can perform. A membership's
Owner role in an unrelated business does not grant that business any principal authority.

~~~csharp
public ResourceAudience AudienceFor(TenantRole role, string permission)
{
    if (!For(role).Contains(permission))
        return ResourceAudience.None;

    return role switch
    {
        TenantRole.Owner or TenantRole.Manager or TenantRole.Finance
            => ResourceAudience.TenantResources,
        TenantRole.Staff or TenantRole.Door or TenantRole.Sound
            => ResourceAudience.AssignedResources,
        _ => ResourceAudience.None
    };
}
~~~

Audience is evaluated for the exact requested operation, not once for the whole request. A tenant-wide
grant is insufficient for AssignedResources. A member grant targets MembershipId, not just UserId, so
removal/rejoin does not silently restore assignments. A member grant is sufficient on its own: do not
require a tenant-wide row that would disclose the same resource to other members.

**RBAC follow-on owner:** a separate plan, provisionally Authorization/ComposableRoles, owns multiple
role assignments, customer-defined permission bundles and any role inheritance. Do not add role
hierarchy, deny precedence or a policy engine in this P1 repair. Its required contract is additive,
permission-by-permission union, with TenantResources wider than AssignedResources:

~~~csharp
public sealed record RolePermission(string Name, ResourceAudience Audience);

public static IReadOnlyList<RolePermission> Combine(
    IEnumerable<IReadOnlyList<RolePermission>> roles) =>
    roles.SelectMany(role => role)
        .GroupBy(permission => permission.Name)
        .Select(group => new RolePermission(
            group.Key,
            group.OrderByDescending(permission => permission.Audience).First().Audience))
        .ToArray();
~~~

That follow-on is complete only when invitation/assignment administration, role-change revisions,
last-owner protection and all clients consume multiple assignments. No P1 completion claim includes it.
This is recorded scope with an objective closure condition, not a dependency blocking the six-role repair.

### 4.2 Exact-scope reads, grant ownership and safe responses

Keep six resource grant families, renamed Conversation for the sixth. The nine existing entity
dispositions are:

| Entity | P1 read authority and owning query |
|---|---|
| Application | Summary for a minimal status view; Proposal for deal/proposal details, with terms.read |
| Booking | Summary for status; Operations for permitted operational details; no implicit Contract access |
| Contract | Read with terms.read, independently checked on JSON and PDF routes |
| Concert | Summary, Operations and Finance queried separately; Finance requires settlement.view |
| Invoice | Read with settlement.view, irrespective of Artist/Venue business activity |
| ConcertAvailability | Bounded availability/conflict projection; no private booking IDs, parties or prices |
| Message | Exact Read grant on its Conversation; no independent pair columns |
| ConversationReadPosition | Matching MembershipId plus current Read access to that Conversation |
| ContentReport | Current reporter membership and ReporterUserId for status; separately authorized moderation for evidence/notes |

Before, the parent matches any scope and the DTO includes private fields:

~~~csharp
ConcertAccessGrants.Any(grant =>
    grant.ResourceId == concert.Id && grant.TenantId == AccessContext.TenantId
    && grant.RevokedAt == null);

new ConcertDetails { DoorRevenue = concert.DoorRevenue, InvoiceId = invoiceId };
~~~

After, put common membership/audience/time restrictions on each typed grant query filter, with the
scope's permission restriction in that same expression. Parent/child filters reference those grants
in one direction; grants never navigate through their filtered parent. This is the required expanded
Concert grant expression, using context-instance properties so EF caches no tenant-specific constant:

~~~csharp
builder.Entity<ConcertAccessGrant>().HasQueryFilter(grant =>
    Access.Membership != null
    && MembershipAuthority.Any(member =>
        member.MembershipId == Access.Membership.MembershipId
        && member.TenantId == Access.Membership.TenantId
        && member.UserId == Access.Membership.UserId
        && member.PermissionVersion == Access.Membership.PermissionVersion)
    && grant.TenantId == Access.Membership.TenantId
    && grant.RevokedAt == null
    && grant.ValidFrom <= Access.UtcNow
    && (grant.ValidUntil == null || Access.UtcNow < grant.ValidUntil)
    && (
        ((grant.Scope == ConcertAccessScope.Summary
          || grant.Scope == ConcertAccessScope.Operations)
         && (OperationsAudience == ResourceAudience.TenantResources
             && (grant.MembershipId == null
                 || grant.MembershipId == Access.Membership.MembershipId)
             || OperationsAudience == ResourceAudience.AssignedResources
                && grant.MembershipId == Access.Membership.MembershipId))
        || (grant.Scope == ConcertAccessScope.Finance
            && (FinanceAudience == ResourceAudience.TenantResources
                && (grant.MembershipId == null
                    || grant.MembershipId == Access.Membership.MembershipId)
                || FinanceAudience == ResourceAudience.AssignedResources
                   && grant.MembershipId == Access.Membership.MembershipId))
    ));

builder.Entity<ConcertEntity>().HasQueryFilter(concert =>
    ConcertAccessGrants.Any(grant =>
        grant.ResourceId == concert.Id && grant.Scope == ConcertAccessScope.Summary));
~~~

OperationsAudience and FinanceAudience are context properties backed by AudienceFor(operations.view)
and AudienceFor(settlement.view). Express the shared membership/audience/time expression once in
DataAccess's expression builder and substitute its parameter into each family expression; do not call
an arbitrary C# predicate inside a SQL filter or use Compile/Invoke/client evaluation. The expanded
expression above is the required SQL semantics, including exact member matching on the same grant.
Use the same expression owner for direct grant queries, counts, includes, exports and commands.
The shared composition helper combines expression bodies instead of introducing an invocation:

~~~csharp
internal static Expression<Func<T, bool>> And<T>(
    Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
{
    var body = new ParameterReplacer(right.Parameters[0], left.Parameters[0])
        .Visit(right.Body)!;
    return Expression.Lambda<Func<T, bool>>(
        Expression.AndAlso(left.Body, body), left.Parameters);
}

private sealed class ParameterReplacer(
    ParameterExpression source, ParameterExpression target) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node) =>
        node == source ? target : base.VisitParameter(node);
}
~~~

DataAccess owns the common membership/audience/time expression; each module supplies its scope policy
and combines it with And. ResourceAccessContext.UtcNow calls TimeProvider for each query execution,
not once when the request or EF model was constructed.
Give operational/finance assignees a Summary row too; principal issuance explicitly supplies it.

Specialized repositories add the exact scope before projecting. A summary type contains no financial
fields or generic entity bag:

~~~csharp
public sealed record ConcertSummary(
    int Id, string Name, DateTime StartsAt, DateTime EndsAt,
    string VenueName, string ArtistName, ConcertState State);

public Task<ConcertSummary?> GetSummaryAsync(int id, CancellationToken ct) =>
    context.Concerts.Where(concert => concert.Id == id)
        .Select(concert => new ConcertSummary(
            concert.Id, concert.Name, concert.Period.Start, concert.Period.End,
            concert.Venue.Name, concert.Artist.Name, concert.State))
        .SingleOrDefaultAsync(ct);

public IQueryable<ConcertEntity> WithFinanceAccess() =>
    context.Concerts.Where(concert =>
        context.ConcertAccessGrants.Any(grant =>
            grant.ResourceId == concert.Id && grant.Scope == ConcertAccessScope.Finance));
~~~

Venue and Artist in this query are Concert's existing local read models, not cross-module aggregates.
No DoorRevenue, TicketsSold, Deal,
ContractId, InvoiceId, download URL, payment or mutation link belongs in ConcertSummary.
ApplicationSummary likewise excludes Opportunity.Deal and proposal text. Remove generic private
detail endpoints that return the full entity through a summary grant. Separate routes return Summary,
Operations, Terms and Finance projections and apply their respective permission/grant checks.

Tenant owns a keyless authority view instead of lending a writable aggregate table:

~~~csharp
builder.Entity<MembershipAuthority>().HasNoKey();
builder.Entity<MembershipAuthority>().ToView("MembershipAuthority", "tenant");
~~~

~~~sql
CREATE VIEW tenant.MembershipAuthority AS
SELECT Id AS MembershipId, TenantId, UserId, PermissionVersion
FROM tenant.Memberships;
~~~

Create the view in Tenant's regenerated InitialCreate; resource contexts exclude it from their migrations.
Expose IQueryable<MembershipAuthority> internally, not a public writable DbSet. EF tracking/writing a
keyless type must fail. This does not make all other writes safe: privileged repositories and the fence
below are mandatory for resource mutation; query filters do not constrain arbitrary attached updates.

Principal issuance is explicit and participates in the resource transaction. Never use
Enum.GetValues to grant future scopes automatically. Contract's constructor currently has no issuance;
add it to the constructor/factory path used by every concrete contract:

~~~csharp
private void IssuePrincipalGrants(DateTime at)
{
    foreach (var tenantId in new[] { VenueTenantId, ArtistTenantId }.Distinct())
        accessGrants.Add(ContractAccessGrant.Issue(
            tenantId, membershipId: null, ContractAccessScope.Read,
            ResourceGrantKind.Principal, at));
}
~~~

Attach the child through the aggregate navigation so EF supplies the generated ResourceId; do not
pretend a not-yet-generated int key is a valid persisted FK. Apply explicit allowlists to all six
families and preserve immutable economic identities until P2 replaces them.

### 4.3 Every mutation uses the command policy

Before, an endpoint permission and a filtered entity load suffice:

~~~csharp
var application = await repository.GetByIdAsync(id, ct);
application.Withdraw(at);
await unitOfWork.SaveChangesAsync(ct);
~~~

After, the handler obtains a fenced actor, requires the operation's exact grant/audience, then applies
the aggregate's business actor and lifecycle policy. API actions call that same policy on read facts;
the command repeats it under the fence. An action returned earlier is never reusable authorization.

~~~csharp
var actor = await authorizationFence.RequireMembershipAsync(snapshot, ct);
if (actor.TryGetError(out var authorityError))
    return authorityError;

var application = await privilegedRepository.GetByIdForUpdateAsync(id, ct);
if (application is null)
    return new WithdrawApplicationError.ApplicationNotFound(id);

if (!membership.HasPermission(TenantPermission.ApplicationsSubmit)
    || snapshot.TenantId != application.ArtistTenantId)
    return new WithdrawApplicationError.NotPermitted();

if (!await access.CanActAsync(
        id, ApplicationAccessScope.Proposal,
        TenantPermission.ApplicationsSubmit, snapshot, ct))
    return new WithdrawApplicationError.NotPermitted();

return application.Withdraw(at);
~~~

The added NotPermitted case belongs to the operation's closed error union. Reuse the actual aggregate
transition result; do not return success after a rejected transition. GetByIdForUpdateAsync is an
internal privileged-repository query; CanActAsync evaluates the exact operation's access facts.
Both use the command transaction and the lock order in 4.5. The service owns the authorization decision.
An endpoint attribute is a useful early check, not the only enforcement.

| Command | Required business actor in P1 | Member permission and grant |
|---|---|---|
| Apply / withdraw | Applicant Artist tenant | applications.submit; existing application needs Proposal |
| Accept / reject / cancel application | Opportunity's Venue tenant | applications.decide; Proposal |
| Cancel booking | Either current accepted principal | bookings.cancel; Booking Operations; existing cancellation-state/payment policy |
| Edit/post concert, declare door revenue, check in | Venue tenant responsible for current operations | Exact concerts operation permission; Concert Operations; finance entry additionally requires Finance |
| Cancel concert | Either current accepted principal | concerts.manage; Concert Operations; existing cancellation-state/payment policy |
| Read/download contract | Any explicitly admitted tenant, presently the two principals | terms.read; Contract Read |
| Read/download invoice or financial dashboard | Eligible finance member of an admitted tenant | settlement.view; exact Invoice Read or Concert Finance; no venue-profile gate |
| Share concert summary | Either current principal | resources.share; principal entitlement |
| Assign an operational member | Principal assigning its own current membership | resources.share; allowed operational scopes only; no finance/terms widening |

P1 introduces member assignments only inside a current principal tenant, so Staff/Door/Sound can
perform assigned work. It does not admit an external Summary recipient to an operational assignment.
This keeps future accepted third-business responsibilities in P2 rather than silently inventing them
inside a read-sharing endpoint.

~~~csharp
public UnitResult<AssignConcertMemberError> AssignMember(
    Guid actorTenantId, Guid membershipId, Guid membershipTenantId, DateTime at)
{
    if ((actorTenantId != VenueTenantId && actorTenantId != ArtistTenantId)
        || membershipTenantId != actorTenantId)
        return new AssignConcertMemberError.NotPermitted();

    AddMemberGrant(membershipId, actorTenantId, ConcertAccessScope.Summary, at);
    AddMemberGrant(membershipId, actorTenantId, ConcertAccessScope.Operations, at);
    return new Success();
}
~~~

Tenant's membership lookup validates and fences the exact target membership before this call.
The aggregate owns AddMemberGrant and checks duplicates. Revocation uses the same resource fence.
No public grant setter/Revoke method lets a caller skip principal/issuer policy. The shared base is
in another assembly, so use a protected base operation and an internal concrete operation:

~~~csharp
public abstract class ResourceAccessGrant<TScope>
{
    public Guid? MembershipId { get; protected set; }
    public ResourceGrantKind Kind { get; protected set; }
    public DateTime? RevokedAt { get; protected set; }
    public long Version { get; protected set; }

    protected void RevokeCore(DateTime at)
    {
        if (RevokedAt is not null)
            return;
        RevokedAt = at;
        Version++;
    }
}

public sealed class ConcertAccessGrant : ResourceAccessGrant<ConcertAccessScope>
{
    internal void Revoke(DateTime at) => RevokeCore(at);
}
~~~

Retain the base's other existing identity/validity/issuer properties and protected initialization.
Each concrete Domain assembly's aggregate can call its own internal Revoke; application callers cannot.

Delete ApplicationSide's binary fallback. The replacement response has Actions computed from each
specific operation, with false for an unrelated tenant; do not force it into an Artist/Venue view.
Contract/PDF links appear only after a terms policy decision. Financial dashboard queries must be
separate from operational summaries and enforce settlement.view at controller and service boundaries.

### 4.4 Concert summary sharing only, with explicit replay and expiry behavior

Remove Application and Booking share/revoke routes, requests, response/error unions and aggregate
methods added by this branch. Keep their grant families for principal reads. Keep Contract and Invoice
unshareable in P1. Conversation audience creation is its own policy in 4.7, never a generic share route.

Before:

~~~csharp
private static readonly ConcertAccessScope[] ShareableScopes =
    [ConcertAccessScope.Summary, ConcertAccessScope.Operations];

public sealed record ShareConcertRequest(
    Guid ToTenantId, Guid? ToMemberUserId,
    ConcertAccessScope Scope, DateTime? ValidUntil);
~~~

After, the request cannot select a wider scope:

~~~csharp
public sealed record ShareConcertSummaryRequest(
    Guid RequestId,
    Guid RecipientTenantId,
    Guid? RecipientMembershipId,
    long ExpectedAccessVersion,
    DateTime? ValidUntil);

public sealed record ConcertSummaryShare(
    Guid GrantId, long GrantVersion, long AccessVersion,
    Guid RecipientTenantId, Guid? RecipientMembershipId, DateTime? ValidUntil);
~~~

~~~text
POST   /api/concert/{id:int}/summary-shares
DELETE /api/concert/{id:int}/summary-shares/{grantId:guid}?expectedVersion={version}
POST   /api/concert/{id:int}/member-assignments
DELETE /api/concert/{id:int}/member-assignments/{membershipId:guid}
~~~

The closed share errors are NotPermitted, ConcertNotFound, InvalidRecipient, InvalidValidity,
AlreadyShared, Superseded and RequestConflict. Validate nonempty IDs, an existing eligible recipient
tenant, exact active recipient membership when supplied, ValidUntil > the decision time and the
expected resource access version. No recipient Artist/Venue profile is required. Never call a
constructor that throws for ordinary request validation.

The aggregate checks the principal and fixes the scope itself:

~~~csharp
public Result<ConcertAccessGrant, ShareConcertSummaryError> ShareSummary(
    Guid issuerTenantId, Guid issuerUserId,
    Guid recipientTenantId, Guid? recipientMembershipId,
    DateTime at, DateTime? validUntil)
{
    if (issuerTenantId != VenueTenantId && issuerTenantId != ArtistTenantId)
        return new ShareConcertSummaryError.NotPermitted();

    if (validUntil is { } until && until <= at)
        return new ShareConcertSummaryError.InvalidValidity();

    if (accessGrants.Any(grant =>
        grant.Kind == ResourceGrantKind.SharedSummary
        && grant.IssuedByTenantId == issuerTenantId
        && grant.TenantId == recipientTenantId
        && grant.MembershipId == recipientMembershipId
        && grant.RevokedAt == null))
        return new ShareConcertSummaryError.AlreadyShared();

    var grant = ConcertAccessGrant.IssueSummaryShare(
        recipientTenantId, recipientMembershipId, issuerTenantId, issuerUserId, at, validUntil);
    accessGrants.Add(grant);
    AccessVersion++;
    return grant;
}
~~~

ResourceGrantKind is Principal, SharedSummary or MemberAssignment. Principal grants cannot be revoked
by either of these management routes. Each principal owns its own explicit share: unique active
issuance includes IssuedByTenantId and Kind. Revoking one issuer's share does not revoke an independent
share from another issuer or a principal entitlement; the response must not claim otherwise.

Maintain separate unique filtered indexes for tenant and member audiences:

~~~sql
CREATE UNIQUE INDEX UX_ConcertGrant_Tenant
ON concert.ConcertAccessGrants
(ResourceId, TenantId, Scope, Kind, IssuedByTenantId)
WHERE RevokedAt IS NULL AND MembershipId IS NULL;

CREATE UNIQUE INDEX UX_ConcertGrant_Membership
ON concert.ConcertAccessGrants
(ResourceId, TenantId, Scope, Kind, IssuedByTenantId, MembershipId)
WHERE RevokedAt IS NULL AND MembershipId IS NOT NULL;
~~~

Keep tenant-first read indexes too. Expiry cannot be a moving filtered-index predicate. Under the
resource lock, revoke an expired matching issuance and flush that update before inserting its
replacement, within the same still-uncommitted transaction:

~~~csharp
concert.RevokeExpiredSummaryShares(issuerTenantId, recipient, decisionTime);
await command.SaveChangesAsync(ct);
var result = concert.ShareSummary(
    issuerTenantId, issuerUserId, recipient.TenantId, recipient.MembershipId,
    decisionTime, request.ValidUntil);
~~~

Validate and check the durable request receipt before this sequence. A live matching issuance returns
AlreadyShared; it is not extended silently. Same RequestId/same payload returns its recorded response;
same RequestId/different payload returns RequestConflict. Persist the receipt with the grant in this
transaction, keyed by issuing tenant, operation and RequestId. Resource AccessVersion changes whenever
its grants change; grant Version changes on revocation. Expected versions participate in the command
decision. Locking prevents normal uniqueness races; translate any remaining known unique constraint
conflict by rereading the receipt/issuance in a fresh transaction, not by returning an untyped 500.

The summary disclosure rule is deliberately narrow: either current principal may share engagement
identity, schedule, public profile display names and lifecycle status for coordination. Operational
data, proposal terms, financial records and conversation history have no equivalent P1 widening rule.
Adding any of those is a future consent/assignment design, not another enum value in this route.

### 4.5 A command owns one local transaction; reads own independent connections

Before:

~~~csharp
services.AddScoped<DbConnection>(_ => new SqlConnection(connectionString));
options.UseSqlServer(provider.GetRequiredService<DbConnection>());
await Task.WhenAll(applicationQuery, bookingQuery, concertQuery);
~~~

The first two lines bind the existing parallel dashboard calls to one connection. Replace that
registration. Ordinary AddDbContext registrations use their own connection string. Command handlers
obtain contexts through an internal CommandTransaction, not the request's already-resolved contexts.

The coordinator begins before resolving a handler. Its context factory supplies the same open
SqlConnection and explicitly enlists every participating DbContext in the same DbTransaction:

~~~csharp
public TContext Enlist<TContext>(IServiceProvider services)
    where TContext : DbContext
{
    var options = new DbContextOptionsBuilder<TContext>()
        .UseSqlServer(Connection, contextOwnsConnection: false)
        .Options;
    var context = ActivatorUtilities.CreateInstance<TContext>(services, options);
    context.Database.UseTransaction(Transaction);
    participants.Add(context);
    return context;
}
~~~

Each module registers its privileged repositories against its single enlisted context instance;
standalone query operations retain their ordinary filtered contexts. No module facade returns a DbContext.
ApplicationPrivilegedDbContext, BookingPrivilegedDbContext, ConcertPrivilegedDbContext and the existing
ConversationsPrivilegedDbContext compose PrivilegedDbContext and their module's configuration provider.
They map complete owning aggregates and ACLs without recipient filters. Internal XPrivilegedRepository
implementations provide that persistence stance; fenced application services and the system workflows
in 4.6 are their authorized consumers. System and interactive commands use the same module stance;
their service entry points enforce different explicit policies.

This separate command mapping is necessary: the interactive grant filter deliberately hides shares
addressed to someone else and expired rows, so it cannot administer the complete ACL. Do not use
IgnoreQueryFilters or a flag on the interactive context. Membership and the exact operation are
authorized under locks before loading that full ACL:

~~~csharp
var member = await authority.RequireCurrentAsync(snapshot, ct);
if (member.TryGetError(out _))
    return new ShareConcertSummaryError.NotPermitted();

var identity = await privilegedRepository.GetIdentityByIdForUpdateAsync(id, ct);
if (identity is null)
    return new ShareConcertSummaryError.ConcertNotFound(id);

if (!sharePolicy.Allows(snapshot, identity))
    return await access.CanReadSummaryAsync(snapshot, id, ct)
        ? new ShareConcertSummaryError.NotPermitted()
        : new ShareConcertSummaryError.ConcertNotFound(id);

var concert = await privilegedRepository.GetWithGrantsByIdAsync(id, ct);
~~~

This is the sharing service's command path. RequireCurrentAsync validates the exact snapshot against
the locked authoritative role/version; sharePolicy requires resources.share and a principal tenant.
GetIdentityByIdForUpdateAsync returns only the resource's principal IDs/access version and holds its
update lock. GetWithGrantsByIdAsync includes the full grant collection, intentionally loading recipient,
expired and revoked ACL rows only after the service authorizes this actor. Ordinary recipient reads
remain filtered. Other command services apply their own exact operation policy and visibility;
repositories implement the keyed queries and locking, not the business decision.

All membership, eligibility, recipient-membership, resource and grant queries used for command
authorization execute on this same enlisted connection/transaction. A module facade inside a command
must resolve its command fact/authority port, never an independent ordinary read context. Independence
applies to standalone reads such as dashboards, not to the evidence on which a protected write relies. Context construction
and enlistment occur after BeginTransaction, not in a DbContext base constructor or an options callback.

~~~csharp
await executionStrategy.ExecuteAsync(async () =>
{
    await using var scope = scopeFactory.CreateAsyncScope();
    await using var command = await CommandTransaction.BeginAsync(
        connectionString, IsolationLevel.ReadCommitted, ct);
    scope.ServiceProvider.GetRequiredService<CommandTransactionAccessor>().Set(command);

    var handler = scope.ServiceProvider.GetRequiredService<THandler>();
    var result = await handler.ExecuteAsync(request, ct);

    if (result.TryGetError(out _))
    {
        await command.RollbackAsync(ct);
        return result;
    }

    await command.FlushAsync(ct);
    await command.ValidateAuthorityAsync(ct);
    await command.CommitAsync(ct);
    return result;
});
~~~

CommandTransactionAccessor is scoped, set once, has no AsyncLocal/static state and exposes no privilege.
Nested handlers in this scope join it; they may flush but may not commit, rollback independently,
create another TransactionScope or start concurrent EF operations. A nested error makes the root
result fail and the whole command rolls back. FlushAsync runs the existing domain-event/outbox save
pipeline for every enlisted participant until no new events/context changes remain; it must not
replace that pipeline with a bare SaveChanges call that skips events. Register each context only once,
dispose all participants before the owned connection, and keep HTTP/external provider calls outside.

Allocate RequestId/operation identity before executionStrategy; retries create a fresh DI scope,
connection, transaction and all contexts. A durable receipt resolves an ambiguous commit on retry.
Do not retry a database action on a previously tracked factory context. Keep current reserve →
external Payment call → outcome/complete separation: first commit durable intent and operation identity,
then perform Payment/blob I/O, then apply the outcome in a fresh fenced transaction. Reuse that same
provider operation identity on ambiguity/retry; a database command receipt alone does not make an
external charge or upload replay-safe.
An explicit command failure rolls back even when represented as a Result, not thrown as an exception.

ArtistDashboardService and OpportunityDashboardService retain useful parallelism by opening independent
read scopes for their module calls. Ordinary module contexts no longer share a request connection.
Within a command, cross-module calls are sequential and enlisted. Test both modes; disabling all
parallelism across HTTP requests is not this design.

**Outbox correction:** the installed platform package is 0.2.0-alpha.0.5, source
3136a4ee10be52dbea6a9badc56e3140b025ff20. Its writer already does:

~~~csharp
var context = accessor.Context ?? throw new InvalidOperationException();
context.Set<OutboxMessageEntity>().Add(message);
~~~

DbContextBase maps the outbox entity outside module migration ownership;
OutboxUnitOfWorkBehavior selects that business context. Preserve this behavior and ensure its selected
context is one of the enlisted participants. The dispatcher's OutboxDbContext can use its own
connection after commit. The uncommitted provider-aware AddOutbox overload in platform-dotnet is
neither proof of atomicity nor a required fix for ordinary business insertion. Leave that sibling
work untouched. Verify rollback with business rows and their outbox rows, not two mock commits.

For protected writes, Tenant owns narrow fence queries; modules do not mutate borrowed membership
rows. Use this consistent order across all competing commands:

1. Relevant tenant rows in tenant-ID order; shared HOLDLOCK reads for read-only eligibility,
   UPDLOCK/HOLDLOCK from initial acquisition for tenant settings/activity/deletion writes and
   membership administration/last-owner decisions.
2. Exact membership rows in ID order, including target memberships; retain shared authority locks
   through commit, and use update locks for membership changes.
3. Resource rows in a fixed module/key order, with UPDLOCK/HOLDLOCK before mutation.
4. Grant/assignment rows and then command receipts; all revocation/assignment paths take the same
   resource lock before changing its grants.

~~~sql
SELECT Id, TenantId, UserId, Role, PermissionVersion
FROM tenant.Memberships WITH (HOLDLOCK)
WHERE Id = @MembershipId AND TenantId = @TenantId AND UserId = @UserId;

SELECT Id, AccessVersion
FROM concert.Concerts WITH (UPDLOCK, HOLDLOCK)
WHERE Id = @ConcertId;
~~~

Acquire sorted keys sequentially using explicit key reads; a single IN query does not guarantee
lock acquisition order. Declare write intent before acquisition: never take shared locks on rows
this command will later update and rely on lock conversion. Compare the authoritative role/version
under these locks; do not trust only cached HasPermission.
Grant validity is rechecked at the final authorization decision after flushing and immediately before
commit. Record that decision time; time cannot be locked. A revocation committed before this decision
denies it; a competing revocation waits until the authorized transaction completes. Expiry is evaluated
at this explicit decision instant, not an earlier HTTP request timestamp. Do not promise recall of
bytes already read or immunity to clock passage between the final SQL check and physical COMMIT.

Raw/bulk writes are allowed only in the owning internal repositories behind these checked services; ban generic
ExecuteUpdate/Delete and attached resource mutations from interactive consumers. Enforce module
construction boundaries and test attempts to bypass them. Query filters alone are not a write fence.

### 4.6 System work, public reads, moderation and fixtures have named capabilities

Delete IExecutionScope, IExecutionScopeActivator, ExecutionScope, ExecutionPurpose and the request
middleware/host setup that enters them. Remove IsHost from resource predicates. Absence of a human
never changes an interactive DbContext into an unrestricted context.

Before:

~~~csharp
using var scope = executionScope.Enter(ExecutionPurpose.System);
await workflow.CompleteAsync(concertId, ct);
~~~

After, keep normal ConcertDbContext filtered and use section 4.5's privileged stance for system
workflows. It composes the same module mapping without interactive filters. Use the existing
mapping-only base; the context's name does not grant access to any caller:

~~~csharp
internal sealed class ConcertPrivilegedDbContext(
    DbContextOptions<ConcertPrivilegedDbContext> options,
    ConcertConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<ConcertEntity> Concerts => Set<ConcertEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
}
~~~

Only the module's privileged repositories, matching unit-of-work carriers and seed factories may
inject that context. Controllers and ordinary query services cannot resolve it or a privileged
repository. Register the write stance through the command coordinator; completion scans use the
read-only stance in their own scope. No per-request flag changes either model's visibility.

Keep ISettlementService/SettlementService as the orchestration owner of ReserveAsync, CompleteAsync
and RecordFailureAsync. It checks settlement state, eligibility and operation identity over repositories
inside the command boundary. Those operations do not belong on a persistence interface.
Concert persistence belongs to IConcertPrivilegedRepository; invoice persistence belongs to
IInvoicePrivilegedRepository. Follow the existing capability bases and module-local aliases:

~~~csharp
internal abstract class PrivilegedRepository<TEntity>(ConcertPrivilegedDbContext context)
    : Repository<TEntity, int>(context)
    where TEntity : class, IIdEntity;

internal interface IConcertPrivilegedRepository : IRepository<ConcertEntity>
{
    Task<ConcertEntity?> GetByIdForUpdateAsync(int concertId, CancellationToken ct = default);
    Task<ConcertEntity?> GetWithGrantsByIdAsync(int concertId, CancellationToken ct = default);
}

internal interface IInvoicePrivilegedRepository : IRepository<InvoiceEntity>
{
    Task<bool> ExistsByBookingIdAsync(int bookingId, CancellationToken ct = default);
}
~~~

Each concrete implementation derives from PrivilegedRepository<TEntity> and adds only its entity's
queries; inherited CRUD is not redeclared. Add GetIdentityByIdForUpdateAsync from 4.5 to the concert
interface with the minimal principal/version result. Give InvoiceSequenceEntity its own
InvoiceSequenceRepository. InvoiceIssuer orchestrates allocation and insertion through those repositories, without taking a
DbContext parameter. Apply module-local aliases to every unit-of-work carrier used by this stance.

Move GetEndedPendingCompletionIdsAsync from IConcertRepository to IConcertReadRepository, adding an
explicit due-time cutoff and batch limit. CompletionRunner consumes that existing read repository;
a completion-only wrapper interface would duplicate one query already owned by the aggregate.

Replace these exact consumers:

| Existing consumer | Replacement wiring |
|---|---|
| Infrastructure/Services/Completion/CompletionRunner | IConcertReadRepository candidate query; fresh command scope per candidate; invoke the internal completion workflow |
| SettlementService and its FactoryUnitOfWork<ConcertDbContext> boundary | Keep ISettlementService; inject IConcertPrivilegedRepository and join the command's enlisted unit of work with fresh-scope retries; preserve operation claims/state transitions |
| InvoiceIssuer | Inject IInvoicePrivilegedRepository and the InvoiceSequenceEntity repository; check existence, allocate and insert in the settlement command's transaction |
| Self-billing agreement and tenant eligibility reads used by settlement | Entity-owned repositories/fact queries keyed by the explicit settlement tenants and enlisted in the command transaction; request filtering supplies no system authority |
| SettlementPaymentProcessor and failure processor | Resolve the concert through IConcertPrivilegedRepository; ISettlementService validates the operation and applies the outcome with inbox receipt and activity outbox atomically |
| ConcertApiFixture seed/setup/assertion helpers | Seed factories use explicit setup scopes; assertions use the read stance; actual access tests invoke interactive contexts/HTTP with real membership |

A payment outcome must never disappear because the interactive repository returned no row. Its
single database command is:

~~~csharp
if (await inbox.ContainsAsync(envelope.MessageId, handlerName, ct))
    return new Success();

var concert = await privilegedRepository.GetByIdForUpdateAsync(concertId, ct);
if (concert is null)
    return new SettlementOutcomeError.TargetMissing(concertId);

if (concert.SettlementOperationId != operationId)
    return new SettlementOutcomeError.OperationMismatch(concertId, operationId);

var result = await settlement.CompleteAsync(concertId, operationId, ct);
if (result.TryGetError(out var error))
    return error;

inbox.Record(envelope, handlerName);
await activities.RecordSettlementAsync(concert, envelope, ct);
return new Success();
~~~

TargetMissing/OperationMismatch produce the existing durable failed-message/retry handling and an
observable diagnostic, not a success inbox receipt. A separately defined stale-outcome policy may
record an explicitly ignored outcome; it must prove the target/operation relationship first.
The success receipt, invoice, state transition and outbox either commit together or all roll back.

For tests, seed access and interactive access use separate scopes:

~~~csharp
await fixture.SeedAsync(async setup => await setup.CreateConcertAsync(seed, ct));
using var client = fixture.CreateClientFor(memberUserId, activeTenantId);
var response = await client.GetAsync($"/api/concert/{concertId}/summary", ct);
await fixture.RunCompletionAsync();
~~~

SeedAsync is not an ambient scope around the test. It issues normal principal grants and exposes no
permission to the client. Direct persisted-state assertions use a clearly named fixture read helper,
not a fake human or a fixture-wide System scope. A test of filtering must exercise the filtered
context even if the same test also runs the worker.

Public browsing gets a published-only projection through the read context:

~~~csharp
public Task<PublishedConcert?> GetPublishedByIdAsync(int id, CancellationToken ct) =>
    context.Concerts
        .Where(concert => concert.Id == id && concert.DatePosted != null)
        .Select(concert => new PublishedConcert(
            concert.Id, concert.Name, concert.About,
            concert.Period.Start, concert.Period.End,
            concert.Venue.Name, concert.Artist.Name, concert.Price))
        .SingleOrDefaultAsync(ct);
~~~

Define PublishedConcert separately from every private DTO and use it for listing/history queries too.
DatePosted is the source's existing publication criterion; completion/settlement must not accidentally
hide published history by requiring State == Posted. Unpublished drafts, private details and invoices
never enter this projection. Qualification includes direct-ID browsing of an unpublished
concert; a controller business-profile requirement does not substitute for publication.

Keep ConversationsPrivilegedDbContext and its entity-owned privileged repositories; the moderation
service is an explicitly authorized consumer. Its command first verifies current server-owned admin authority for the
authenticated UserId. Reporter reads return only their submitted report/status; moderation evidence,
reporter identity and internal notes use a different DTO and current moderation permission.
Normal conversation membership grants no right to another reporter's evidence.

### 4.7 Conversations have an identity, an audience and a monotonic read position

Rename Thread throughout code, schema, routes, client contracts, tests and guidance to Conversation.
There is no adapter or second table name. Replace pair/participant lookup with explicit creation and
subsequent ID-addressed commands.

Before:

~~~csharp
var thread = await threadRepository.GetByParticipantsAsync(tenantIds, ct);
thread ??= ThreadEntity.Create(tenantIds, at);
await SendAsync(thread.Id, message, ct);
~~~

After:

~~~csharp
public sealed record CreateConversationRequest(
    Guid RequestId, IReadOnlyList<Guid> ParticipantTenantIds);

public sealed record SendMessageRequest(
    Guid RequestId, string Content);

public sealed record ConversationResponse(
    int ConversationId, IReadOnlyList<ConversationParticipant> Participants);

public sealed record MessageResponse(
    int Id, int ConversationId, long Sequence,
    Guid SenderTenantId, Guid SentByUserId, string Content, DateTime SentAt);
~~~

~~~text
POST /api/conversations
GET  /api/conversations/{conversationId:int}
GET  /api/conversations/{conversationId:int}/messages
POST /api/conversations/{conversationId:int}/messages
PUT  /api/conversations/{conversationId:int}/read-position
~~~

Creation requires messages.send with TenantResources audience, a current creator membership and a
distinct validated set containing the active tenant and at least one other existing tenant. Validate
the explicit recipient IDs through Tenant's contract; do not require a marketplace activity to receive
messages or expose an unrestricted tenant directory. AssignedResources permits sending to assigned
conversations, not creating tenant-wide conversations. Use a stable creation RequestId with a unique (CreatorTenantId, CreatedByMembershipId, RequestId)
receipt and a payload hash. Replay returns the same ID; changed payload conflicts. Distinct creation
requests may deliberately create separate conversations between the same businesses. Sending requires
the existing ConversationId and its own request receipt; do not recreate participant-set deduplication.

The initial participant set is immutable in P1. Remove public AddParticipant from this slice: admitting
someone to existing private history needs its own consent/history policy. Creation issues explicit
Read and SendMessages grants; do not grant future enum members automatically. Principal tenants can
assign their own current staff memberships to this conversation through the same protected member-
assignment pattern; no API can add an unrelated tenant by changing grant rows.

The Read grant must match permission, current MembershipId, tenant, validity and scope together.
Rename Participate to SendMessages. A SendMessages grant does not itself satisfy Read:

~~~csharp
builder.Entity<MessageEntity>().HasQueryFilter(message =>
    ConversationAccessGrants.Any(grant =>
        grant.ResourceId == message.ConversationId
        && grant.Scope == ConversationAccessScope.Read));
~~~

ConversationAccessGrants already carries the common same-row membership/audience/time restrictions
from 4.2, with messages.read/messages.send selected per scope. Remove ThreadIdsOf's separate incomplete
predicate. For member-assigned readers/senders, explicitly issue both needed scopes; neither one is
inferred from the other. Message sends also lock the conversation and apply messages.send before insert.

Replace timestamp read watermarks with a per-conversation sequence. Appending acquires the conversation
row lock, increments LastMessageSequence and inserts that sequence in the same transaction. Unique
(ConversationId, Sequence) and message RequestId indexes protect replay. Holding the parent lock through
commit avoids a later sequence committing before an earlier message.

~~~csharp
public long AllocateMessageSequence() => ++LastMessageSequence;

public sealed record AdvanceConversationReadPositionRequest(long ThroughSequence);
~~~

A read response contains each delivered sequence. A caller advances only their own current membership's
position through an existing readable message; the request cannot supply an arbitrary future sequence.
Do not mark DateTime.UtcNow as read, which could skip a concurrently delivered message. Under the
conversation/membership fence, perform an atomic first-insert/maximum update:

~~~sql
SELECT @Current = LastReadSequence
FROM conversations.ConversationReadPositions WITH (UPDLOCK, HOLDLOCK)
WHERE ConversationId = @ConversationId AND MembershipId = @MembershipId;

IF @Current IS NULL
    INSERT conversations.ConversationReadPositions
        (ConversationId, TenantId, MembershipId, LastReadSequence)
    VALUES (@ConversationId, @TenantId, @MembershipId, @ThroughSequence);
ELSE IF @Current < @ThroughSequence
    UPDATE conversations.ConversationReadPositions
    SET LastReadSequence = @ThroughSequence
    WHERE ConversationId = @ConversationId AND MembershipId = @MembershipId;
~~~

Unique (ConversationId, MembershipId) supports the range lock. Two tabs cannot regress the position
or fail a first insert. A rejoined member has a different position/assignment identity. Content reports
retain MessageId, ConversationId, ReporterTenantId and ReporterUserId; rename ReportedByUserId,
which currently describes the reporter ambiguously.

Replace profile-event participant display overwrites with one Tenant-owned projection:

~~~csharp
public sealed record TenantDisplayChanged(
    Guid TenantId, long Version, string DisplayName);

public sealed record ConversationParticipant(Guid TenantId, string DisplayName);
~~~

Tenant supplies a nonempty display name from its tenant settings (LegalName when set, otherwise its
creation DisplayName). Persist DisplayVersion, increment it when the effective display changes, and
raise TenantDisplayChanged at creation and each such change. The projection applies only a greater
version and ignores older events. Artist/Venue updates never
overwrite the tenant identity. ConversationResponse lists all participants; delete CounterpartTenantId
and the first-other-member pattern rather than guessing one representative of a group.

Before, MessageService sends a full MessageDto to all members and writes its content to activity:

~~~csharp
await notifier.MessageReceivedAsync(userId, messageDto);
activity.Subject = content;
~~~

After, outbox delivery re-resolves current authorized recipients and sends an invalidation only:

~~~csharp
public sealed record ConversationChanged(int ConversationId);

foreach (var recipient in await recipients.GetCurrentReadersAsync(conversationId, ct))
    await notifier.ConversationChangedAsync(recipient.UserId, new(conversationId), ct);
~~~

The authenticated fetch rechecks access. Recipient selection uses the same exact Read policy, including
membership incarnation and scope validity. Remove message activity records from the tenant-wide activity feed entirely; it must contain no
message text, excerpt, participant list or conversation existence notification. The messages surface
computes its unread indicator from authorized conversation/read-position queries. Revoke subscriptions on authority change and close
them on tenant switch. Already delivered bytes cannot be recalled.

### 4.8 Neutral business lifecycle and real client consumption

The current new activity rows are not marketplace profiles. Rename TenantBusinessProfileEntity/Kind
to TenantBusinessActivityEntity/Kind (VenueOperator, Artist, Promoter); keep actual Artist/Venue profile
types distinct. Rename RequiresBusinessProfile to RequiresBusinessActivity on profile-specific work.
A profile-independent shared summary, invoice, contract or invitation route must not acquire that
attribute. Retiring an activity prevents new work; it does not remove rights to an accepted record.

Before, every provisioning client supplies an Artist/Venue kind:

~~~csharp
if (client.Client.InitialBusinessProfile is { } kind)
    tenant.ActivateBusinessProfile(kind, now);
~~~

After, include a real Business browser/mobile registration journey in the identity client roster,
with no mandatory marketplace activity:

~~~csharp
public sealed record CreateTenantRequest(
    string DisplayName, string ContactEmail,
    IReadOnlyList<TenantBusinessActivityKind> Activities);

public sealed record UpdateTenantRequest(
    string LegalName, string ContactEmail, TaxComplianceDto TaxCompliance);
~~~

Tenant creation persists tenant + Owner membership + chosen activity rows atomically; an empty list is
valid. The selected list is validated server-side, not inferred from tokens or an arbitrary client
string. Add tenant settings commands to activate/retire each known activity, protected by
tenant.settings.edit and current Tenant.EligibilityVersion. Where activation requires a profile's
details, that module's existing profile setup command creates/validates those details; an activity
row does not fabricate a Venue/Artist aggregate. Render every applicable tab together.

TenantService.UpdateAsync calls both UpdateLegalDetails and UpdateContactEmail, with server-side email
validation and the expected tenant version. Contact, legal name and verification status are Tenant
data; rename BusinessFacts to TenantBusinessDetails. The membership repository contract and query
ownership are specified in 4.1. Rename Tenant.AuthorityVersion to EligibilityVersion
and Membership.AuthorizationVersion to PermissionVersion; changes increment the version whose
meaning they actually affect.

Tenant deletion acquires the tenant administration fence, refuses live resources/financial obligations
through the existing owning-module checks, and explicitly removes its own activities, invitations and
memberships before deleting an otherwise deletable tenant. It never cascades through another module's
grants or frozen financial records. Translate CannotDeleteWithLiveObligations to a typed response;
the newly introduced restricted activity FK is not an acceptable runtime error.

Close the retained role-escalation path at invitation issue AND acceptance:

~~~csharp
public static bool CanAssignRole(TenantRole actor, TenantRole target) =>
    actor == TenantRole.Owner
    || actor == TenantRole.Manager
       && target is TenantRole.Staff or TenantRole.Door or TenantRole.Sound;
~~~

Persist inviter MembershipId, target role and invitation version. Acceptance rechecks the inviter's
current assignment authority and the invited identity under the tenant fence; a revoked/demoted inviter
cannot mint an Owner later. Only Owner changes roles/removes members outside the manager invitation
allowlist. Serialize owner-count decisions on the tenant administration lock so two concurrent removals
or demotions cannot leave zero owners.

Complete the actual neutral web flow at app/web/business, not its static marketing gateway:
authentication, tenant selection, invitation acceptance, settings and shared concert summaries.
The emitted /settings/members/accept/{invitationId} path must resolve there and work without an Artist/
Venue profile. Use shared authenticated providers and route components; keep marketing on its public
route. Mobile BusinessTabs must contain those operational journeys rather than PlaceholderScreen.

Before, navigation chooses one profile:

~~~tsx
return hasVenue ? <VenueTabs /> : hasArtist ? <ArtistTabs /> : <BusinessTabs />;
~~~

After, compose neutral navigation and all eligible surfaces, filtered by server permissions:

~~~tsx
<BusinessNavigator
  sections={[
    { key: "operations", component: OperationsScreen },
    ...(hasVenue ? [{ key: "venue", component: VenueNavigator }] : []),
    ...(hasArtist ? [{ key: "artist", component: ArtistNavigator }] : []),
    ...(canReadMessages ? [{ key: "messages", component: ConversationsScreen }] : []),
  ]}
/>
~~~

Do not add role-to-permission computation back into app/shared. Membership carries Permissions plus
MembershipId/PermissionVersion and activities; resource responses carry the operations authorized by
their server policy. UI flags are presentation only. Admin verification uses its actual wire contract:

~~~ts
export interface PendingVerification {
  readonly tenantId: string;
  readonly legalName: string;
  readonly contactEmail: string;
  readonly submittedAt: string;
}
~~~

Delete VerificationTenantBusinessProfile and the nonexistent name/email/businessProfile reads.
Update every existing web/mobile consumer in the same cutover.

Tenant switching is a session boundary, not invalidateQueries over unscoped keys. Private query keys
start with tenant and membership identity, and a session generation prevents old work affecting the
new selection:

~~~ts
const summaryKey = (session: TenantSession, id: number) =>
  ["tenant", session.tenantId, session.membershipId,
    session.permissionVersion, "concert", id, "summary"] as const;

async function selectTenant(tenantId: string) {
  tenantSession.beginSwitch();
  await queryClient.cancelQueries({ predicate: isPrivateQuery });
  await subscriptions.close();
  await pendingCommands.settleOrRecordReceipts();
  queryClient.removeQueries({ predicate: isPrivateQuery });
  const session = await tenantSession.select(tenantId);
  await subscriptions.open(session);
  tenantSession.completeSwitch(session);
}
~~~

beginSwitch increments the generation and hides the private route tree. Each request captures that
generation and sends the captured TenantId header; it must not read a mutable tenant header after
starting. A response/callback from an older generation is discarded. Disallow new mutations during
switch; existing mutations finish against their captured tenant or retain their request receipt for
reconciliation. Aborting an HTTP request does not prove its server mutation was cancelled.
completeSwitch remounts only after the new membership is resolved. Membership/grant-change notifications
and stale-authority responses run the same generation/cancellation/cache-clear boundary for affected
private data and refresh membership before remounting; do not silently replay a rejected mutation.
Implement the same contract in mobile; do not copy divergent cache rules into each screen.

### 4.9 Naming inventory and dependency boundary

Apply this inventory to introduced types and their members, enum values, DbSets, FKs, migrations,
routes, wire DTOs, seeds, tests and clients together. Names not changed below keep the repository's
existing Entity/Repository/Configuration conventions; there is no parallel old spelling.

| Reviewed name/family | Disposition |
|---|---|
| ThreadEntity, IThreadRepository, ThreadRepository, ThreadAccessGrant/Scope, ThreadId, Threads | ConversationEntity, IConversationRepository, ConversationRepository, ConversationAccessGrant/Scope, ConversationId, Conversations |
| ThreadReadStateEntity / LastReadAt | ConversationReadPosition / LastReadSequence |
| ThreadAccessScope.Participate | ConversationAccessScope.SendMessages |
| GetByParticipantsAsync, CounterpartTenantId | Delete; explicit ConversationId and Participants |
| TenantBusinessProfileEntity/Kind, BusinessProfiles, RequiresBusinessProfile, BusinessProfileAuthorizationFilter | TenantBusinessActivityEntity/Kind, BusinessActivities, RequiresBusinessActivity, BusinessActivityAuthorizationFilter |
| MembershipFact | MembershipSnapshot |
| IMembershipFacts / MembershipFacts | Authorization.Contracts.IMembershipReadRepository implemented by Tenant's existing MembershipRepository; move the queries there and delete MembershipFacts (4.1) |
| ActiveMembership / MembershipResolution | Keep a single MembershipSnapshot plus MembershipResolution; no duplicate snapshot record |
| MembershipAuthorityFact / ConfigureBorrowedRelations | MembershipAuthority / ConfigureMembershipAuthority |
| AuthorizationVersion / AuthorityVersion | PermissionVersion / EligibilityVersion |
| AccessContext, IAccessContext, IHasAccessContext, AccessScopedDbContext | ResourceAccessContext, IResourceAccessContext, IHasResourceAccessContext, ResourceScopedDbContext |
| DesignTimeAccessContext | DesignTimeResourceAccessContext; no membership, no host bypass |
| GrantOrigin.ResourceCreation / ExplicitShare | ResourceGrantKind.Principal / SharedSummary; add explicit MemberAssignment |
| ToTenantId / ToMemberUserId, grant MemberUserId | RecipientTenantId / RecipientMembershipId; grant MembershipId |
| BusinessFacts | TenantBusinessDetails |
| ApplicationDetailsDto.Application + binary ApplicationSide | Exact ApplicationSummary/Proposal response and authorized Actions; delete ApplicationSide |
| ApplicationTenants | ApplicationPrincipals only where both IDs are actually needed; retain VenueTenantId/ArtistTenantId member names |
| SharedConnectionExtensions | Delete request-wide connection registration; CommandTransaction/CommandTransactionAccessor own command wiring |
| System completion/settlement persistence | Keep ISettlementService/SettlementService and CompletionRunner; use IConcertReadRepository for scans and entity-owned XPrivilegedRepository implementations for writes, with module-local context and unit-of-work aliases |
| Privileged context/repository names | Use XPrivilegedDbContext over PrivilegedDbContext and XPrivilegedRepository; retain ConversationsPrivilegedDbContext. Service methods name intent; repository methods name the queried entity/key |
| ExecutionScope/Purpose, IsHost access flag | Delete as described in 4.6 |
| ShareApplication/ShareBooking requests, mappers and domain/application error families | Delete with those P1 surfaces |
| ShareConcertRequest/Response and duplicated generic sharing errors | ShareConcertSummaryRequest / ConcertSummaryShare and operation-specific closed error unions |
| ResourceAccessGrant<TScope>, six concrete grant families, Scope, ResourceId, ValidFrom/Until, RevokedAt, Version | Keep: each names its exact job; base mutation is protected, concrete mutation internal |
| TenantRole/TenantPermission, IPermissionCatalog, HasPermissionAttribute, permission policy/provider/handler | Keep; names describe member authorization and their contracts change as above |
| FrontendSurface, MembershipDto, MembershipContext/Accessor, MembershipResolution, UserMembership, QueryableTenantMappers | Keep; request-resolution and internal query names have concrete jobs |
| MessageSenderKind.Org / MessageSender.Org | Tenant / Tenant(...) in backend contracts |
| ParticipantProfile conversation projection | TenantDisplay projection; actual conversation participants remain ConversationParticipant |
| ReportedByUserId | ReporterUserId |
| BusinessTabs / TabsForProfiles | BusinessNavigator / composed sections; actual Artist/Venue navigators remain |
| VerificationTenantBusinessProfile | Delete; verification is a tenant legal/contact record |
| SettlementPayerTenantId / SettlementPayeeTenantId and InvoiceParty | Keep the two independent axes and reserved invoice value |

Remove design-narration comments introduced by the branch. Do not write comments explaining the rename
or contrasting it with the rejected implementation; document that reasoning here and in commits.
Only a necessary local invariant/workaround can justify a code comment under the global rule.

**Finbuckle decision:** do not migrate this P1 to Finbuckle.MultiTenant. Its documented shared-database
EF mechanism assigns one TenantId to an entity and enforces tenant mismatch on writes. That is useful
single-owner tenancy infrastructure, but it does not decide many-tenant resource audiences, disclosure
scopes, current member permissions, revocation fences or accepted financial actors. Adopting it would
leave the substantive policy in this section necessary and require adapting active membership
resolution. A future library proposal must demonstrate how it composes with this resource policy.

Keep single-owner profile filtering and shared resource authorization separate. Reconsider a library
migration only in a dedicated proposal demonstrating reduced single-owner plumbing without bypassing
resource policy; it is not P1 prerequisite or claimed work. Primary references:
[Finbuckle EF Core integration](https://github.com/Finbuckle/Finbuckle.MultiTenant/blob/main/docs/EFCore.md),
[EF global filters](https://learn.microsoft.com/en-us/ef/core/querying/filters) and
[EF cross-context transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions#cross-context-transaction).

### 4.10 Qualification matrix for the replacement

These are implementation acceptance requirements, not tests executed by this documentation change.
Use SQL Server with independent connections/barriers for races. Retain
existing lifecycle and economic behavior tests; do not make access failures green with a broad bypass.

| Area | Required observable proof |
|---|---|
| Membership/filter translation | No human/tenant denies; malformed explicit tenant errors; cached EF model works across two tenants; keyless authority rows cannot be tracked/written; exact scope/audience/time restrictions appear in SQL |
| Membership incarnation | Resolve Owner request; remove; rejoin same user as Staff at version 1; old request still denies. New membership does not inherit old member assignments/read position |
| Principal issuance | Every Application/Booking/Contract/Concert/Invoice/Conversation principal receives the explicit initial scopes in the creating transaction; both contract JSON/PDF principals work with terms.read |
| External sharing | A separate zero-profile business reads only the shared concert summary; raw JSON contains no financial/terms/private IDs; no Operations, Contract, Invoice, Message, mutation or resharing access even for its Owner |
| Assigned members | Staff/Door/Sound cannot use a principal's tenant-wide grants; exact member assignment admits only permitted operations. Another member, removed/rejoined member and wrong tenant deny |
| Read surfaces | Lists, single reads, counts, includes, dashboards, exports, PDFs and attachments obey the same scope; unpublished direct-ID public lookup returns no private resource |
| Command authority | Foreign Owner cannot withdraw/reject/cancel application, cancel Booking/refund, edit/post/cancel Concert or enter door revenue; genuine principals retain the valid transitions |
| Finance | Weak venue member cannot read invoice/dashboard money; authorized artist supplier can. VenueHire payer/payee and supplier/customer direction remains correct |
| Shares | Invalid IDs/recipient/expiry typed errors; exact replay returns one receipt/grant; changed replay conflicts; expired reissue succeeds; two simultaneous issuers and duplicate requests have specified outcomes; stale version conflicts |
| Revocation | Force both commit orders for member downgrade/removal, member-assignment revocation and share revocation versus protected writes. Grant expiry between load and final decision denies |
| Membership administration | Concurrent owner demotions/removals preserve one Owner; Manager cannot invite/promote Owner/Manager/Finance; inviter revocation before acceptance denies elevated issuance |
| Transaction | ACL administration sees external/expired/revoked grant rows only after actor authorization; all command authorization facts use the enlisted transaction; current Application acceptance→Booking→Contract→outbox rolls back entirely on injected failure; nested Result error rolls back; no independent nested commit; ambiguous retry returns same receipt from fresh contexts |
| Parallel reads | Existing Artist and Opportunity dashboard Task.WhenAll paths work on distinct connections; no concurrent contexts within one command connection |
| Workers | No-Human completion, reserve, invoice creation and payment success/failure operate correctly; duplicate outcome idempotent; unknown target/operation is observable and not falsely success-receipted |
| Fixture isolation | Same fixture can seed, run workers and independently assert interactive filtering; removing any Enter(System) wrapper does not disable seed or settlement setup |
| Conversations | Two intentional conversations with same participants remain distinct; replay creation/send does not duplicate; group response has every participant; Read and Send scope/audience cannot be assembled from mismatched rows |
| Read position | Concurrent first insert and reverse-order advances remain monotonic; append sequences commit in order; members keep independent positions; future/unreadable sequence cannot be submitted |
| Reports and delivery | Reporter/status and moderator/evidence isolation; unauthorized tenant members receive no message content via SignalR or activities; revoked recipient fails delivery/read reauthorization |
| Business lifecycle | Neutral registration, promoter-only business, activity activation/retirement and contact update are reachable; deletable tenant cleans activities, live obligations refuse without FK error |
| Web | Actual Business invitation acceptance and shared summary with separate users; both Artist/Venue sections for a multi-activity tenant; admin verification legal/contact fields render |
| Mobile/session switch | Real emulator/device evidence for neutral and multi-activity navigation, assigned member, invitation and tenant switch; delayed old queries/subscriptions/mutations cannot contaminate the new session |

Run focused backend build/unit/integration/architecture/startup checks for changed owners; regenerate
the five InitialCreate migrations/snapshots and synthetic fixtures. Replace the filter-string test
with model coverage plus the security/provider cases above. Run existing build:web/build:mobile and
the applicable browser E2E workflow on the implementation candidate. CI's ordinary PR workflow does
not itself run the separate Api/Ui E2E tiers; browser evidence does not qualify native navigation.

P1 stays incomplete until this matrix passes on the actual replacement candidate, the touched
guidance describes the resulting source and review findings close. The historical branch test counts,
a TypeScript build and the existence of grants do not establish any of those conclusions.

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
RequirementAccessGrant(EvidenceRead/Submit/Decide) follow section 4's typed FK/scope/member restrictions
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
cardinality and required capability/input scopes. P2 provides Payer and Payee slots for the direct
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
Lists are bounded/keyset-paged. Downloads return bytes/content type/name after scope authorization.

| Producer/operation | Exact input and consumed output | Boundary |
|---|---|---|
| Tenant membership/activity read, P1 | Active validated membership → TenantId, MembershipId, Role, PermissionVersion, BusinessActivities, Permissions | Synchronous HTTP JSON for switcher/navigation; replace type-driven DTOs in the same slice |
| Concert summary share/revoke, P1 | ConcertId, recipient TenantId/optional MembershipId, validity, expected access/grant version, RequestId → ConcertSummaryShare | Fixed Summary scope; exact routes/replay/issuer policy in 4.4 |
| Concert summary read, P1 | ConcertId → ConcertSummary: name, period, location/artist public labels and state | No mutation action, fee, payment, invoice ID, contract/blob reference or unrelated participant list; operations use a separate authorized query |
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
owns acceptance orchestration. Tenant, Show, entry and Booking command contexts, including the outbox
rows staged on those contexts, enlist in **one connection and DbTransaction** through section 4.5's
command coordinator. Module contracts expose operations,
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
evidence while future delegated actions fail. Expiry is checked at the final authorization decision
time under the section 4.5 fence and that decision time is recorded. Race tests
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

For SQL Server, persist the exact schema-versioned canonical snapshot/envelope
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

Production members get only their evidence/operational scopes. Assignment requires membership and
input visibility; a missing grant is a validation failure, not implicit disclosure. Assignment changes
within the agreed approver principal require that principal's resource-management permission.
Changing the contractual approver principal or effect needs renewed agreement and is unsupported
after acceptance in this foundation.

Replacing evidence or reassigning the reviewer reopens current satisfaction. Decisions form immutable
audit history: each pins the assignment and evidence version; concurrent replacement wins or loses
under the fence. Unrelated requirements retain their own outcomes. If publication already happened,
new evidence records outstanding review/change state; it cannot undo that publication, repeat its
external effect or silently alter fees. Any already-performed consequence follows a later supported
change capability.

One Show may share a rider reference but each affected agreement has its own accepted requirement and
approval decision. Reading shared evidence does not reveal underlying artist contracts.

## 9. Implementation phases and verification

P1 is implemented and under terminal qualification/delivery; P2–P5 remain outstanding.
Each phase ends with its entire exposed behaviour usable and secure,
a focused green candidate, review and applicable exact-head delivery checks. Commits may divide work
inside a phase; do not expose half a resource's security cutover. Combine adjacent phases in one PR
where dependencies permit; package publication is the real reason for a separate delivery boundary.

### P1 — repair and qualify resource access with neutral business consumption

**Current verdict:** the P1 replacement is implemented and its accepted canonical code findings are closed.
Section 4 remains the governing specification. Current-graph qualification, final canonical review,
exact-head remote validation and merge remain before P1 is delivered.

Implement in these cohesive slices, each with the relevant section 4.10 acceptance:

1. Membership incarnation/version and audience-aware permissions; remove RestrictedParticipant and
   ambient execution privilege; keyless authority view and exact-scope projections/principal grants.
2. Command transaction/enlistment and the authoritative write fence; actor checks, role administration,
   safe Concert Summary sharing and member assignment; remove Application/Booking sharing.
3. Purpose-specific processing/publication/moderation consumers; completion/settlement/invoice/inbox
   atomicity and independently scoped fixture setup; preserve active-business-context outbox insertion.
4. Conversation identity, sequence/read position, tenant display and safe notification/activity delivery.
5. Neutral onboarding/activity/contact/invitation flows, real web/mobile surfaces, finance/admin contract
   corrections, complete tenant-switch isolation and the naming/InitialCreate/guidance cutover.

**Consumed result:** a separate business without a Venue/Artist profile reads the explicitly shared
ConcertSummary and nothing beyond it. Current principals and specifically assigned members retain
only their policy-authorized operations. Shared Summary never implies accepted responsibility,
financial entitlement, signing or cancellation authority; P2 supplies the accepted participant model.

**Done when:** all section 4.10 cases are proven, current lifecycle/economic tests remain green,
browser/native journeys work and a review of the committed replacement closes every F01–F28 finding.
The current authorization carries P1 through review, push, exact-head remote validation and merge.
The companion ledger owns those live gates; P2–P5 remain outside this delivery.

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
Replace its producers, deserializers and consumers together. Add no private participant, signature,
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

The sibling head/PR observations below are dated 15 September 2026. Recheck branch heads, dirty paths
and PR heads before implementing; do not treat these as current merge-state assertions or write in
a sibling checkout.

| Owner | Observed state | Implementation versus delivery |
|---|---|---|
| Refactor/DealVocabularyAndMapperCollapse | Local acd4a6ed69559e03d8fd1458d6edb55fce3d822c; dirty TECH_DEBT.md; no branch PR observed | 7d0e3024 supplies OperationClaim/owned mappings; 34f5042b supplies in-process AttemptVerdict/AttemptAsync; 8d17865a removes the unused settlement resolver family. Consume these exact commits and reconcile overlapping files before P1/P2 delivery; do not recreate their work or trust the stale ledger's claim that implementation is absent |
| Feature/MonorepoBacklogPort | Local d2613b7d7d3a7e88ea05747f9a42c20beffe7126; PR #14 remote c4e1e88e3d2c181410c4b695e09deb141465f8aa; eleven generated UI test files dirty | Reconcile HasLiveObligationsAsync, human erasure and ActionLink changes against P1/P2. Its erasure does not retire tenants/accounts; its shared ActionLink still interpolates routes. “Recovered/approved” commercial design is not runtime implementation authority or proof it landed |
| Fix/CiCompleteCheckContext | Local/PR #15 c5993e497939cbb8e71186a725703a541164f031 | Required CI-context repair; verify landed before relying on the merge gate. Does not block local design/implementation |
| Central Deal Configuration | Economic language/configuration owner | Pure language/evaluator design can consume this attachment contract now. Runtime participants require delivered P1–P2; representative use P3; both routes P4; evidence-bound step P5 |
| Central PostgreSQL migration | Shared preparation exists; B2B provider swap separate | Resolve the owner's landed database choice before implementation, then regenerate and test that single persistence shape. The SQL Server examples describe the reviewed branch's storage requirements; translate those constraints to the selected database if it changes |
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
| 3. Authorization placement | Neutral Authorization module exists; ManagerClients still owns provisioning client selection | Keep the dependency inversion, complete section 4's audience/freshness/consumer rules; no module imports Tenant solely for permission policy |
| 4. Workers in-memory transport | Live: Workers/ServiceCollectionExtensions.cs; Web owns real subscriptions | Host transport owner retains general relocation. Foundation handlers must use the qualified durable Web/outbox path, or complete the explicit Workers ASB/topology move before assigning them there |
| 5. Dashboard revenue source | Live B2B query in VenueDashboardService; fixture mock returns zero. External recorder diagnosis not re-proved | Sales-evidence owner replaces Payment reporting with authoritative B2B facts and nonzero assertions. Gate any advertised promoter revenue dashboard on that work; operational summary does not claim revenue |
| 6. Throwaway checkout ID | Live AuthorizeAsync(Guid.CreateVersion7()); endpoint is already POST | P2 fixes durable commitment identity/replay; correct the debt's GET claim |
| 7. Missing ConcertSalesProjection | Gross/refund projection absent; TicketPurchasedEvent and TicketSaleProcessor already exist with inbox/count updates | Customer/B2B sales owner defines recorded gross/refund basis; remove the stale event-existence blocker. Required before verified sales/shared-show settlement, not participant access |
| 8. E2E source-reference split | Old premise resolved: AppFixture uses pinned external images/packages; suite remains in B2B | Correct the entry in the guidance pass. Keep actual per-PR versus scheduled/manual tiers explicit; central system relocation remains its own owner |
| 9. Duplicate seed-host DI | Resolved in source: B2BTestClient seeding and E2E Web AddB2BWebHost | Remove stale item after focused composition evidence; do not create another seed host |
| 10. Dashboard query on write repository | Resolved by OpportunityReadRepository and module-facade composition | Remove stale item after confirming current callers; P2 Show dashboard uses proper read projections |
| 11. Tenant hard-delete teardown | TenantService.DeleteAsync now also conflicts with the new restricted activity FK | P1 removes its local memberships/invitations/activities only after live-obligation checks permit deletion (4.8). Tenant retirement/account teardown remains separately owned; never cascade accepted records or leave usable dangling authority |
| 12. Duplicate Venue/Artist surface | Separate marketplace profile queries remain; Tenant contact facts now replace the keyed contact resolver | Keep module-owned profile reads; finish neutral activity/contact consumption and do not infer tenant identity from a profile |
| 13. Seed TicketsSold/Payment simulator | Existing resolution confirmed in Seed.Infrastructure/Factories/ConcertFactory.cs | Keep deterministic synthetic sale facts; no Payment seed dependency to restore |
| 14. Handwritten action URLs | Live Api mappers; sibling only shares ActionLink record. Prefix handling lives in published web package | P2/P4 changed route links use route generation and round-trip execution assertions. Broader route/web package cleanup retains its owner; update stale paths |
| 15. Admin contact N+1 | Tenant-owned contact replaces keyed profile dispatch; the admin client still reads the obsolete DTO | P1 fixes the client contract and proves bounded Tenant queries for the verification queue; retain batching debt only if an actual N+1 remains |
| 16. Nullable application actions | Binary ApplicationSide and state-only links admit wrong actor assumptions | P1 computes actions from the command policy and removes the outsider-as-Venue fallback (4.3); P2/P4 extend the same contract to accepted participants |
| 17. Contract PDFs in images/check-then-upload | Live Booking ContractPdfRenderer + Blob ContainerName images | P2 private document storage, immutable snapshot rendering and create-only idempotent writes; verify concurrent render and scope download denial |
| 18. Contract minting only by convention | Private setters/internal factories already exist; MintContract remains unguarded assignment | P2 acceptance-only aggregate factory/seal and database immutability close residual issue; remove false public-mutation claims |
| 19. Decimal fees | Live DealTerms/domain values converted to Money.Gbp at boundary | Deal Configuration owns typed currency semantics; preserve actual current amount/rounding here, never mix currencies through a binding |
| 20. Format-only tax verification | Live UkTaxComplianceRules regex; old class names stale | Tax-evidence owner retains authoritative verification and live-release decision; foundation captures provenance without asserting verified status |
| 21. Portal URLs | Base config supplies localhost; no checked-in production override, runtime override unverified | P1/P4 new invitation links use configured origins. Operations owner must provide/test actual environment values before customer invitation release; correct the false always-empty-dictionary diagnosis |
| 22. Thin Admin/no UI | Bare admin profile remains; moderation SPA, roster and provisioning already exist | P1 retains separate privileged moderation authority and tests report isolation. Correct absent-UI claims; richer admin roles/caching remain admin owner |
| 23. Conversation/read/retention | Thread aggregate and member timestamp state exist at the reviewed head but have identity, authorization and concurrency defects | P1 replaces them with section 4.7's ConversationId/sequence/read-position mechanism. Retention periods remain policy-owned; no arbitrary purge is inferred |
| 24. Message-only report | Live MessageId/report pair; no second target demanded | P1 removes pair access and protects report scopes, retains typed MessageId. New content targets wait for a concrete feature and its module-owned report contract |
| 25. Satellite repositories | MessageRepository owns the existing thread read-state writes; ConcertImages also lacks an explicit owned-child exception | P1 adds the narrow ConversationReadPosition repository with the specified atomic command; touched guidance states the actual Concert image child boundary |
| 26. Web payment-method callback | Published @concertable/web 0.1.0-alpha.0.6627 still exposes the ID; B2B ignores it | Web producer's own publish/bump cleanup; new P2/P4 callers post signatures/references only. Do not add raw payment-method IDs to B2B contracts |
| 27. E2E output length | Tests still use bin; e2e.yml hardcodes Playwright path. Old measured root/local-platform.ps1 stale | Repair actual output discovery if affected worktree/build exceeds limits; retain infrastructure owner and remeasure before asserting numbers |
| 28. TestEntity seed handles | Live published TestKit/SeedState.cs | P2/P4 new handles use concrete types; consolidate touched existing handles with the B2B package closure, not another generic placeholder |
| 29. Checkout base amount versus payer total | Live response gap; old charge examples not reverified | Payment pricing owner supplies authoritative quote fields; P2/P4 label agreed fee separately and never calculate platform fees. No claim of fully disclosed live charging until that producer/consumer gate closes |
| 30. Unfixed image-package exception | Live scripts/VulnerabilityGate.ps1, narrowly scoped | Image owner retains evidence/current upstream check and removes exception when fixed; no foundation-specific suppression or stale vulnerability-count claim |
| 31. PublishContainer remote default | Live Directory.Build.props ContainerRegistry ghcr.io | Build/release owner makes default local and CI destination explicit; all foundation validation uses archive output, never a publish side effect |
| 32. Historical client-key disclosure | Current env files blank; rotation/restriction not evidenced | External console owner closes recorded disclosure decision before public release; do not print keys or treat blank current files as rotation |

Before configuration runtime integration, P1/P2 must close access/authorization placement, unsafe
identity inference, operation-claim inconsistency, unstable checkout identity, accepted immutability
and the guidance that routes those changes. Conversation access/read-state structure closes in P1.
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
  members work through current membership, resource and scope checks across every exposed channel.
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
directly. Delete superseded models and rebuild the synthetic fixtures from the replacement schema.

Record final implementation, review and validation in the ledger at substantive boundaries. Keep it
while delivery remains open. At terminal delivery, update the service delivery index, move durable
rules to their owning guidance, discharge all downstream handoffs, and delete this plan and ledger.
Git history retains the design; unfinished later product capabilities retain their
own owners and completion gates.
