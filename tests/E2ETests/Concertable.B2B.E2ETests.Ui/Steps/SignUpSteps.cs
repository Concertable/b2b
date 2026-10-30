using Concertable.B2B.E2ETests.Ui.PageObjects;
using Concertable.B2B.E2ETests.Ui.Support;

namespace Concertable.B2B.E2ETests.Ui.Steps;

[Binding]
public sealed class SignUpSteps
{
    private readonly UiFixture fixture;
    private readonly Browser browser;
    private readonly WorkflowState state;

    private LoginPage loginPage = null!;
    private RegisterPage registerPage = null!;
    private CreateVenuePage createVenuePage = null!;
    private CreateArtistPage createArtistPage = null!;

    private string surfaceUrl = null!;

    public SignUpSteps(UiFixture fixture, Browser browser, WorkflowState state)
    {
        this.fixture = fixture;
        this.browser = browser;
        this.state = state;
    }

    [Given(@"a visitor starts sign-up on the venue surface")]
    public async Task VisitorStartsVenueSignUp()
    {
        surfaceUrl = fixture.App.VenueSpaUrl;
        createVenuePage = new CreateVenuePage(browser.Page, surfaceUrl);
        await StartSignUpAsync();
    }

    [Given(@"a visitor starts sign-up on the artist surface")]
    public async Task VisitorStartsArtistSignUp()
    {
        surfaceUrl = fixture.App.ArtistSpaUrl;
        createArtistPage = new CreateArtistPage(browser.Page, surfaceUrl);
        await StartSignUpAsync();
    }

    [When(@"they click the sign up link")]
    public async Task ClickSignUpLink()
    {
        await loginPage.WaitForUrlAsync($"{fixture.App.AuthUrl}/Account/Login**");
        var registrationLoaded = registerPage.WaitForLoadAsync();
        await loginPage.ClickSignUpAsync();
        await registrationLoaded;
    }

    [When(@"they register as (.*)")]
    public async Task RegisterAsUser(string user)
    {
        _ = user;
        state.SignUpEmail = $"signup-{Guid.NewGuid():N}@e2e.test";
        state.SignUpPassword = "P@ssw0rd!";
        await registerPage.RegisterAsync(state.SignUpEmail!, state.SignUpPassword!);
        await registerPage.ClickSignInAsync();
    }

    [When(@"their email verification completes")]
    public Task EmailVerificationCompletes() =>
        fixture.App.WaitForTokenMintingAsync(state.SignUpEmail!, state.SignUpPassword!);

    [When(@"they sign in with their new credentials")]
    public async Task SignInWithNewCredentials()
    {
        await loginPage.WaitForUrlAsync($"{fixture.App.AuthUrl}/Account/Login**");
        await loginPage.SignInAsync(state.SignUpEmail!, state.SignUpPassword!);
    }

    [When(@"they fill in the create venue form")]
    public async Task FillCreateVenue()
    {
        await createVenuePage.WaitForLoadAsync();
        await createVenuePage.FillAsync(
            name: "E2E Venue",
            about: "Created by an E2E test",
            bannerPath: FixturePath("banner.png"),
            avatarPath: FixturePath("avatar.png"));
    }

    [When(@"they submit the create venue form")]
    public Task SubmitCreateVenue() => createVenuePage.SubmitAsync();

    [When(@"they fill in the create artist form")]
    public async Task FillCreateArtist()
    {
        await createArtistPage.WaitForLoadAsync();
        await createArtistPage.FillAsync(
            name: "E2E Artist",
            about: "Created by an E2E test",
            bannerPath: FixturePath("banner.png"),
            avatarPath: FixturePath("avatar.png"));
    }

    [When(@"they submit the create artist form")]
    public Task SubmitCreateArtist() => createArtistPage.SubmitAsync();

    [Then(@"they land on the venue surface authenticated")]
    public Task LandedOnVenueSurface() =>
        browser.Page.WaitForURLAsync($"{fixture.App.VenueSpaUrl}/", new() { Timeout = 30_000 });

    [Then(@"they land on the artist surface authenticated")]
    public Task LandedOnArtistSurface() =>
        browser.Page.WaitForURLAsync($"{fixture.App.ArtistSpaUrl}/", new() { Timeout = 30_000 });

    private Task StartSignUpAsync()
    {
        loginPage = new LoginPage(browser.Page, surfaceUrl);
        registerPage = new RegisterPage(browser.Page, fixture.App.AuthUrl);
        return browser.Page.GotoAsync(
            $"{surfaceUrl}/login?redirect=%2Fcreate",
            new() { WaitUntil = WaitUntilState.Load });
    }

    private static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
