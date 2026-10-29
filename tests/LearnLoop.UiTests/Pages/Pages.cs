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
