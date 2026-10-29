using LearnLoop.UiTests.Support;
using Microsoft.Playwright.NUnit;

namespace LearnLoop.UiTests.Recorded;

/// <summary>
/// The lab's approach: a sign-in recorded with Playwright Codegen (target "C# NUnit"), kept close to what the
/// recorder produced, plus the two assertions the lab adds. Compare it with Tests/LoginTests.cs to see what
/// changes when a recording becomes a maintainable test.
///
/// Record your own:
///   dotnet run --project src/LearnLoop.Web
///   pwsh tests/LearnLoop.UiTests/bin/Debug/net10.0/playwright.ps1 codegen --target csharp-nunit http://localhost:3000
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class RecordedLoginTests : PageTest
{
    [Test]
    public async Task Admin_can_sign_in_and_reach_the_dashboard()
    {
        // --- recorded by Codegen (only the URL changed: the tests start the app on a free port) ---
        await Page.GotoAsync(AppServer.BaseUrl + "/");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Log in" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("admin@example.com");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("admin12345");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // --- added by hand: a recording has no checks, so without these it would "pass" even if the login failed ---
        // The URL check must come first: the login page ALSO says "Welcome back" (see LoginTests for the trap).
        await Expect(Page).ToHaveURLAsync(new Regex(".*dashboard"));
        await Expect(Page.GetByText(new Regex("Welcome back"))).ToBeVisibleAsync();
    }
}
