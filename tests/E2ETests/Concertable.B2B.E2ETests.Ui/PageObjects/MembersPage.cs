using Concertable.B2B.E2ETests.Ui.Support;

namespace Concertable.B2B.E2ETests.Ui.PageObjects;

public sealed class MembersPage
{
    private readonly IPage page;
    private readonly string spaBaseUrl;

    public MembersPage(IPage page, string spaBaseUrl)
    {
        this.page = page;
        this.spaBaseUrl = spaBaseUrl;
    }

    private ILocator InviteEmail => page.GetByTestId("invite-email");
    private ILocator InviteSubmit => page.GetByTestId("invite-submit");
    private ILocator Roster => page.GetByTestId("members-roster");
    private ILocator Switcher => page.GetByTestId("tenant-switcher");

    public ILocator MemberRow(Guid userId) => page.GetByTestId($"member-row-{userId}");
    private ILocator MemberRolesSave(Guid userId) => page.GetByTestId($"member-roles-{userId}");
    private ILocator RemoveMember(Guid userId) => page.GetByTestId($"remove-member-{userId}");

    public Task GotoAsync() => page.GotoSpaAsync($"{spaBaseUrl}/settings/members");

    public Task WaitForRosterAsync() =>
        Assertions.Expect(Roster).ToBeVisibleAsync(new() { Timeout = 30_000 });

    public Task WaitForSwitcherAsync() =>
        Assertions.Expect(Switcher).ToBeVisibleAsync(new() { Timeout = 30_000 });

    public async Task<Guid> InviteAsync(string email)
    {
        var invited = page.WaitForResponseAsync(response =>
            response.Url.Contains("/api/organization/invitations")
            && response.Request.Method == "POST");
        await InviteEmail.FillAsync(email);
        await page.GetByLabel("Staff").EnsureCheckedAsync();
        await InviteSubmit.ClickAsync();
        var response = await invited;
        var body = await response.JsonAsync();
        return body!.Value.GetProperty("id").GetGuid();
    }

    public async Task AddRoleAsync(Guid userId, string role)
    {
        var row = MemberRow(userId);
        await row.GetByLabel(role).EnsureCheckedAsync();
        await MemberRolesSave(userId).ClickAsync();
    }

    public async Task ExpectRolesAsync(Guid userId, params string[] roles)
    {
        var row = MemberRow(userId);
        foreach (var role in roles)
            await Assertions.Expect(row.GetByLabel(role)).ToHaveAttributeAsync("aria-checked", "true");
    }

    public Task RemoveAsync(Guid userId) => RemoveMember(userId).ClickAsync();

    public async Task SwitchOrganizationAsync(string legalName)
    {
        await Switcher.ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = legalName, Exact = true }).ClickAsync();
    }
}
