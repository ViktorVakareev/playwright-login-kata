namespace LearnLoop.UiTests.Pages;

// Page objects: tests talk about what a user does ("sign in"), the pages know how (which element, which locator).
// Locators use roles and labels, the way a user and a screen reader find things, so they survive CSS and layout changes.

public sealed class HomePage(IPage page)
{
    public ILocator LogInLink => page.GetByRole(AriaRole.Link, new() { Name = "Log in" });
    public ILocator RegisterLink => page.GetByRole(AriaRole.Link, new() { Name = "Register" });

    public Task GotoAsync() => page.GotoAsync("/");
}

public sealed class LoginPage(IPage page)
{
    public ILocator Email => page.GetByLabel("Email");
    public ILocator Password => page.GetByLabel("Password");
    public ILocator SignInButton => page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
    public ILocator Error => page.GetByRole(AriaRole.Alert);
    public ILocator Notice => page.GetByRole(AriaRole.Status);

    public Task GotoAsync() => page.GotoAsync("/login");

    public async Task SignInAsync(string email, string password)
    {
        await Email.FillAsync(email);
        await Password.FillAsync(password);
        await SignInButton.ClickAsync();
    }
}

public sealed class DashboardPage(IPage page)
{
    public ILocator Heading => page.GetByRole(AriaRole.Heading, new() { Level = 1 });
    public ILocator SignOutButton => page.GetByRole(AriaRole.Button, new() { Name = "Sign out" });
    public static readonly Regex Url = new("/dashboard$");

    public Task GotoAsync() => page.GotoAsync("/dashboard");
}

public sealed class RegisterPage(IPage page)
{
    public ILocator Name => page.GetByLabel("Name");
    public ILocator Email => page.GetByLabel("Email");
    public ILocator Password => page.GetByLabel("Password");
    public ILocator CreateAccountButton => page.GetByRole(AriaRole.Button, new() { Name = "Create account" });
    public ILocator Error => page.GetByRole(AriaRole.Alert);

    public Task GotoAsync() => page.GotoAsync("/register");

    public async Task RegisterAsync(string name, string email, string password)
    {
        await Name.FillAsync(name);
        await Email.FillAsync(email);
        await Password.FillAsync(password);
        await CreateAccountButton.ClickAsync();
    }
}

/// <summary>
/// The 3-step sign-up wizard at /join. Located by data-testid: the markup gives each control a stable test hook,
/// so the tests don't break when labels, wording or styling change.
/// </summary>
public sealed class JoinWizardPage(IPage page)
{
    public ILocator Email => page.GetByTestId("register-step-1-email");
    public ILocator ContinueFromEmail => page.GetByTestId("register-step-1-next");
    public ILocator Password => page.GetByTestId("register-step-2-password");
    public ILocator ContinueFromPassword => page.GetByTestId("register-step-2-next");
    public ILocator Name => page.GetByTestId("register-step-3-name");
    public ILocator DateOfBirth => page.GetByTestId("register-step-3-dob");
    public ILocator Region => page.GetByTestId("register-step-3-region");
    public ILocator Plan => page.GetByTestId("register-step-3-tier");
    public ILocator CreateAccount => page.GetByTestId("register-submit-btn");
    public ILocator StepIndicator => page.GetByTestId("register-step-indicator");
    public ILocator StepError => page.GetByTestId("register-error");
    public ILocator ServerError => page.GetByTestId("register-server-error");

    public Task GotoAsync() => page.GotoAsync("/join");

    public async Task CompleteAsync(string email, string password, string name)
    {
        await Email.FillAsync(email);
        await ContinueFromEmail.ClickAsync();
        await Password.FillAsync(password);
        await ContinueFromPassword.ClickAsync();
        await Name.FillAsync(name);
        await CreateAccount.ClickAsync();
    }
}
