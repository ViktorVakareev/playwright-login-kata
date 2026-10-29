using LearnLoop.UiTests.Pages;
using LearnLoop.UiTests.Support;

namespace LearnLoop.UiTests.Tests;

[Category("Registration")]
[Parallelizable(ParallelScope.Self)]
public class RegistrationTests : UiTest
{
    private RegisterPage _register = null!;

    [SetUp]
    public void CreatePage() => _register = new RegisterPage(Page);

    [Test]
    public async Task New_user_can_register_and_then_sign_in()
    {
        // End to end: home → register → confirmation → sign in → personalised dashboard.
        var email = UniqueEmail();
        var home = new HomePage(Page);
        await home.GotoAsync();
        await home.RegisterLink.ClickAsync();

        await _register.RegisterAsync("Ada Tester", email, "correct-horse-1");

        var login = new LoginPage(Page);
        await Expect(login.Notice).ToHaveTextAsync("Account created. Please sign in.");
        await login.SignInAsync(email, "correct-horse-1");
        await Expect(new DashboardPage(Page).Heading).ToHaveTextAsync("Welcome back, Ada Tester");
    }

    [Test]
    public async Task Existing_email_is_rejected_whatever_its_case()
    {
        await _register.GotoAsync();

        await _register.RegisterAsync("Copy Cat", AdminEmail.ToUpperInvariant(), "long-enough-1");

        await Expect(_register.Error).ToHaveTextAsync("An account with this email already exists.");
    }

    [TestCase("short", Description = "7 characters or fewer")]
    [TestCase("1234567", Description = "one under the minimum")]
    public async Task Password_shorter_than_eight_characters_is_rejected(string password)
    {
        await _register.GotoAsync();

        await _register.RegisterAsync("Short Pass", UniqueEmail(), password);

        await Expect(_register.Error).ToHaveTextAsync("Password must be at least 8 characters.");
    }

    [Test]
    public async Task Name_is_shown_as_text_not_run_as_html()
    {
        // A basic XSS check: markup in user input must be displayed, never executed.
        var email = UniqueEmail("xss");
        const string name = "<img src=x onerror=alert(1)>";
        var dialogs = 0;
        Page.Dialog += (_, dialog) => { dialogs++; _ = dialog.DismissAsync(); };
        await _register.GotoAsync();
        await _register.RegisterAsync(name, email, "long-enough-1");

        await new LoginPage(Page).SignInAsync(email, "long-enough-1");

        await Expect(new DashboardPage(Page).Heading).ToHaveTextAsync($"Welcome back, {name}");
        Assert.That(dialogs, Is.Zero, "no script ran");
    }
}
