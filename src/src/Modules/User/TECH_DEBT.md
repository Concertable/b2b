# Concertable.B2B.User — technical debt

## LOW

### `UserEntity.FromRegistration` breaks the codebase's `Create` factory-naming convention

Every other domain entity's static factory in `Concertable.B2B` — `AdminInvitationEntity.Create`,
`ArtistEntity.Create`, `TenantEntity.Create`, `TenantInvitationEntity.Create`,
`TenantMembershipEntity.Create`, `VenueEntity.Create`, `ContractEntity.Create`, `InvoiceEntity.Create`,
and every other one, no exceptions — is named `Create(...)`. `UserEntity.FromRegistration(Guid id, string
email)` is the sole outlier, and it isn't disambiguating anything: `UserEntity` has exactly one
construction path, so there's no sibling `CreateFromImport`/`CreateManually` it needs to distinguish
itself from.

**Resolves when:** renamed to `UserEntity.Create(Guid id, string email)`, updating every call site
(`CredentialRegisteredHandler.HandleAsync`, and the test-only usages in `AdminProvisioningTests`/
`UserProvisioningTests` etc. that borrow it — see also
`api/Concertable.B2B/tests/Concertable.B2B.IntegrationTests.Fixtures/TECH_DEBT.md`'s entry on those same
call sites) in the same change — a symmetric rename, not a half one.
