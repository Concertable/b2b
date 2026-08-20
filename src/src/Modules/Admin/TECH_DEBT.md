# Concertable.B2B.Admin — technical debt

## MED

### `IAdminRepository` mixes two entities instead of one repository per entity

`IAdminRepository`/`AdminRepository` extends `IRepository<AdminInvitationEntity, Guid>` (the generic
base entity bound as `TEntity`) but also hand-implements `CountAdminsAsync`/`ListAdminSubsAsync`/
`IsAdminAsync`/`GrantAdmin`/`RemoveAdmin`, all of which query the unrelated `AdminProfileEntity` via
`context.AdminProfiles` directly. This is exactly the anti-pattern the `dotnet:persistence` skill's
one-repository-per-entity rule names: an interface mixing queries for two unrelated entity types.

Carried over unchanged from the pre-extraction `Concertable.B2B.User` module (where the same shape
already existed under `IUserRepository`'s admin-adjacent members before that copy was split out into
its own interface) — moving the module was a natural point to also split the repository, but the split
was out of scope for a module-boundary extraction and wasn't done. `ITenantRepository` is the
precedent for logging rather than fixing inline: a known, pre-existing violation, not a pattern to copy.

**Resolves when:** split into `IAdminInvitationRepository` (`AdminInvitationEntity`, the current
`Repository<AdminInvitationEntity>` base) and `IAdminProfileRepository` (`AdminProfileEntity`,
`CountAdminsAsync`/`ListAdminSubsAsync`/`IsAdminAsync`/`GrantAdmin`/`RemoveAdmin`), updating
`AdminService` to depend on both narrower interfaces instead of one mixed one.

### `AdminService.InviteAsync` fetches every admin's sub and email just to check one candidate isn't already an admin

`InviteAsync` and `GetOverviewAsync` share a private `ListAdminsAsync` helper (`repository.ListAdminSubsAsync`
+ `userModule.GetEmailsByIdsAsync`, both O(admin count) plus a cross-module payload transfer) — fine for
`GetOverviewAsync`, which needs the full list anyway, but `InviteAsync` only needs a yes/no answer for one
email. `IUserModule` has no email-to-id lookup to resolve the candidate email to a sub directly, so a
narrower check would require adding one — a cross-module contract change, out of scope for the module
extraction this was found during.

**Resolves when:** `IUserModule` gains an email-keyed lookup (e.g. `GetIdByEmailAsync`), and `InviteAsync`
uses it to check `repository.IsAdminAsync(id)` directly instead of fetching every admin.
