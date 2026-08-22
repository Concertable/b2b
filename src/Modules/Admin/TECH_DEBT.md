# Concertable.B2B.Admin — technical debt

## MED

### `AdminService.InviteAsync` fetches every admin's sub and email just to check one candidate isn't already an admin

`InviteAsync` and `GetOverviewAsync` share a private `ListAdminsAsync` helper (`repository.ListAdminSubsAsync`
+ `userModule.GetEmailsByIdsAsync`, both O(admin count) plus a cross-module payload transfer) — fine for
`GetOverviewAsync`, which needs the full list anyway, but `InviteAsync` only needs a yes/no answer for one
email. `IUserModule` has no email-to-id lookup to resolve the candidate email to a sub directly, so a
narrower check would require adding one — a cross-module contract change, out of scope for the module
extraction this was found during.

**Resolves when:** `IUserModule` gains an email-keyed lookup (e.g. `GetIdByEmailAsync`), and `InviteAsync`
uses it to check `repository.IsAdminAsync(id)` directly instead of fetching every admin.
