using LearnLoop.UiTests.Pages;
using LearnLoop.UiTests.Support;

namespace LearnLoop.UiTests.Tests;

[Category("Session")]
[Parallelizable(ParallelScope.Self)]
public class SessionTests : UiTest
{
    [Test]
    public async Task Protected_page_sends_you_to_login_and_back_after_signing_in()
    {
        var dashboard = new DashboardPage(Page);
        await dashboard.GotoAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/login\?ReturnUrl=%2Fdashboard$"));

        await new LoginPage(Page).SignInAsync(AdminEmail, AdminPassword);

        await Expect(Page).ToHaveURLAsync(DashboardPage.Url);
    }

    [Test]
    public async Task Return_url_to_another_site_is_ignored()
    {
        // Open-redirect check: a phishing link must not be able to bounce users to another site after sign-in.
        await Page.GotoAsync("/login?ReturnUrl=https://evil.example.com");

        await new LoginPage(Page).SignInAsync(AdminEmail, AdminPassword);

        await Expect(Page).ToHaveURLAsync(AppServer.BaseUrl + "/dashboard");
    }

    [Test]
    public async Task Sign_out_ends_the_session()
    {
        var login = new LoginPage(Page);
        await login.GotoAsync();
        await login.SignInAsync(AdminEmail, AdminPassword);

        await new DashboardPage(Page).SignOutButton.ClickAsync();
        await Expect(new HomePage(Page).LogInLink).ToBeVisibleAsync();

        await new DashboardPage(Page).GotoAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/login"));
    }

    [Test]
    public async Task Saved_session_skips_the_login_form()
    {
        // Sign in once, save cookies to a file, and start a NEW browser context from it.
        // Real suites do this once per run, so most tests don't spend time on the login form.
        var login = new LoginPage(Page);
        await login.GotoAsync();
        await login.SignInAsync(AdminEmail, AdminPassword);
        await Expect(Page).ToHaveURLAsync(DashboardPage.Url);
        var statePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"admin-state-{Guid.NewGuid():N}.json");
        await Context.StorageStateAsync(new() { Path = statePath });

        await using var signedIn = await Browser.NewContextAsync(new() { BaseURL = AppServer.BaseUrl, StorageStatePath = statePath });
        var page = await signedIn.NewPageAsync();
        await page.GotoAsync("/dashboard");

        await Expect(new DashboardPage(page).Heading).ToHaveTextAsync("Welcome back, Admin");
        File.Delete(statePath);   // the file holds a live session cookie: never keep or commit it
    }
}
