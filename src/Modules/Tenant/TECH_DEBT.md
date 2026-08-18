# Concertable.B2B.Tenant — technical debt

## MED

### `ITenantRepository` mixes three entities instead of one repository per entity

`ITenantRepository`/`TenantRepository` owns `TenantEntity` (the module's `Repository<TenantEntity>`
base) plus hand-written CRUD for `TenantMembershipEntity` and `TenantInvitationEntity` bolted on as
extra interface members. This is the *minority* shape in the codebase: Concert (`ApplicationRepository`,
`BookingRepository`, `ConcertRepository`, `ContractRepository`, `InvoiceRepository`,
`OpportunityRepository`) and Conversations (`MessageRepository`, `MessageAdminRepository`,
`ContentReportRepository`, `ContentReportAdminRepository`) both give every entity — including
closely-related ones in the same module and DbContext — its own repository. `ITenantRepository`
predates that pattern and was the precedent `IUserRepository` mirrored for `AdminInvitationEntity`
before that copy was corrected (see the User module's `AdminRepository` for the one-repository-
per-entity shape this should converge on: base-CRUD entity via the generic `Repository<T>`, satellite
entity's queries hand-written on the *same* repository only when they're small and tightly coupled to
the base entity's lifecycle — never spread across three unrelated entities on one interface).

**Resolves when:** split `ITenantRepository` into `ITenantRepository` (TenantEntity only, base CRUD),
`IMembershipRepository` (TenantMembershipEntity), and `IInvitationRepository` (TenantInvitationEntity),
updating `InvitationService`/`MembershipService`/`TenantService` and their tests to depend on the
narrower interfaces. Coordinate with the `concertable-persistence` skill's "One repository per entity" rule
once added.
