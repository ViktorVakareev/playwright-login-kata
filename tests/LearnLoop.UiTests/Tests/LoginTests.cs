using LearnLoop.UiTests.Pages;
using LearnLoop.UiTests.Support;

namespace LearnLoop.UiTests.Tests;

[Category("Login")]
[Parallelizable(ParallelScope.Self)]
public class LoginTests : UiTest
{
    private LoginPage _login = null!;
    private DashboardPage _dashboard = null!;

    [SetUp]
    public void CreatePages()
    {
        _login = new LoginPage(Page);
        _dashboard = new DashboardPage(Page);
    }

    [Test]
    public async Task Admin_can_sign_in_and_reach_the_dashboard()
    {
        await _login.GotoAsync();

        await _login.SignInAsync(AdminEmail, AdminPassword);

        await Expect(Page).ToHaveURLAsync(DashboardPage.Url);
        await Expect(_dashboard.Heading).ToHaveTextAsync("Welcome back, Admin");   // exact text, in the right element
        await Expect(_dashboard.SignOutButton).ToBeVisibleAsync();
    }

    [TestCase("admin@example.com", "wrongpassword", "Invalid email or password.", Description = "wrong password")]
    [TestCase("nobody@example.com", "admin12345", "Invalid email or password.", Description = "unknown email gets the SAME message")]
    [TestCase("admin@example.com", "ADMIN12345", "Invalid email or password.", Description = "passwords are case-sensitive")]
    [TestCase("", "admin12345", "Enter your email and password.", Description = "empty email")]
    [TestCase("admin@example.com", "", "Enter your email and password.", Description = "empty password")]
    public async Task Invalid_sign_in_shows_an_error_and_stays_on_the_login_page(string email, string password, string message)
    {
        await _login.GotoAsync();

        await _login.SignInAsync(email, password);

        await Expect(_login.Error).ToHaveTextAsync(message);
        await Expect(Page).ToHaveURLAsync(new Regex("/login$"));
        var cookies = (await Context.CookiesAsync()).Select(c => c.Name);
        Assert.That(cookies, Has.No.Member("learnloop.auth"), "a refused sign-in must not create a session");
    }

    [Test]
    public async Task Email_is_case_insensitive_and_trimmed()
    {
        await _login.GotoAsync();

        await _login.SignInAsync("  ADMIN@Example.com ", AdminPassword);

        await Expect(Page).ToHaveURLAsync(DashboardPage.Url);
    }

    [Test]
    public async Task Text_only_check_would_pass_without_signing_in()
    {
        // The lab's "Common issues" warning, proven: "Welcome back" is on the LOGIN page too.
        // A test that only looked for that text could pass even when sign-in failed.
        await _login.GotoAsync();

        await Expect(Page.GetByText(new Regex("Welcome back"))).ToBeVisibleAsync();
        await Expect(Page).Not.ToHaveURLAsync(DashboardPage.Url);
    }

    [Test]
    public async Task Pressing_Enter_in_the_password_field_submits_the_form()
    {
        await _login.GotoAsync();
        await _login.Email.FillAsync(AdminEmail);
        await _login.Password.FillAsync(AdminPassword);

        await _login.Password.PressAsync("Enter");

        await Expect(Page).ToHaveURLAsync(DashboardPage.Url);
    }

    [Test]
    public async Task Failed_sign_in_keeps_the_email_clears_the_password_and_masks_it()
    {
        await _login.GotoAsync();

        await _login.SignInAsync(AdminEmail, "not-the-password");

        await Expect(_login.Email).ToHaveValueAsync(AdminEmail);
        await Expect(_login.Password).ToHaveValueAsync("");
        await Expect(_login.Password).ToHaveAttributeAsync("type", "password");
    }

    [Test]
    public async Task Account_is_locked_after_five_failed_attempts_even_with_the_right_password()
    {
        // Uses its own new account, so the shared admin account is never locked.
        var email = UniqueEmail("lockout");
        var register = new RegisterPage(Page);
        await register.GotoAsync();
        await register.RegisterAsync("Lock Out", email, "right-password-1");

        for (var attempt = 1; attempt <= 5; attempt++)
            await _login.SignInAsync(email, $"wrong-{attempt}");
        await _login.SignInAsync(email, "right-password-1");

        await Expect(_login.Error).ToHaveTextAsync("Too many failed attempts. Your account is locked for 15 minutes.");
        await Expect(Page).ToHaveURLAsync(new Regex("/login$"));
    }
}
