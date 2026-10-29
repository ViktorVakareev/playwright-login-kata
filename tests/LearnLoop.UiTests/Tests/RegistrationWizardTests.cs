using LearnLoop.UiTests.Pages;
using LearnLoop.UiTests.Support;

namespace LearnLoop.UiTests.Tests;

/// <summary>
/// The "Automate the Registration Flow" lab, in C#: a 3-step sign-up wizard (email → password → name),
/// driven by data-testid locators, with unique test data so the test can run again and again.
/// </summary>
[Category("Wizard")]
[Parallelizable(ParallelScope.Self)]
public class RegistrationWizardTests : UiTest
{
    private JoinWizardPage _wizard = null!;

    [SetUp]
    public void CreatePage() => _wizard = new JoinWizardPage(Page);

    /// <summary>The backend rejects an email that already exists, so every run needs a fresh one.</summary>
    private static string TimestampedEmail() => $"qa.learner+{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{Random.Shared.Next(1000)}@example.com";

    [Test]
    public async Task A_new_learner_can_register_through_the_3_step_wizard()
    {
        // Written step by step, as in the lab. A hard-coded email would pass once and then fail on every
        // rerun with "already exists"; the timestamp makes each run create a brand-new account.
        var email = TimestampedEmail();
        await Page.GotoAsync("/join");

        // Step 1: email
        await Page.GetByTestId("register-step-1-email").FillAsync(email);
        await Page.GetByTestId("register-step-1-next").ClickAsync();

        // Step 2: password (at least 8 characters)
        await Page.GetByTestId("register-step-2-password").FillAsync("SecurePass123");
        await Page.GetByTestId("register-step-2-next").ClickAsync();

        // Step 3: name (at least 2 characters), then submit. Date of birth, region and plan are optional.
        await Page.GetByTestId("register-step-3-name").FillAsync("QA Learner");
        await Page.GetByTestId("register-submit-btn").ClickAsync();

        // URL first, then the greeting: "Welcome back" text alone also appears on the login page.
        await Expect(Page).ToHaveURLAsync(new Regex("/dashboard$"));
        await Expect(new DashboardPage(Page).Heading).ToHaveTextAsync("Welcome back, QA Learner");
    }

    [Test]
    public async Task Each_step_appears_only_after_the_previous_one_is_completed()
    {
        await _wizard.GotoAsync();
        await Expect(_wizard.StepIndicator).ToHaveTextAsync("Step 1 of 3");
        await Expect(_wizard.Password).ToBeHiddenAsync();

        await _wizard.Email.FillAsync(TimestampedEmail());
        await _wizard.ContinueFromEmail.ClickAsync();

        await Expect(_wizard.StepIndicator).ToHaveTextAsync("Step 2 of 3");
        await Expect(_wizard.Email).ToBeHiddenAsync();
        await Expect(_wizard.Password).ToBeVisibleAsync();
        await Expect(_wizard.Name).ToBeHiddenAsync();
    }

    [TestCase(1, "not-an-email", "Enter a valid email address.")]
    [TestCase(2, "1234567", "Password must be at least 8 characters.", Description = "one under the minimum")]
    [TestCase(3, "Q", "Name must be at least 2 characters.", Description = "one under the minimum")]
    public async Task Invalid_value_keeps_you_on_that_step_with_a_message(int step, string invalidValue, string message)
    {
        await _wizard.GotoAsync();

        await _wizard.Email.FillAsync(step == 1 ? invalidValue : TimestampedEmail());
        await _wizard.ContinueFromEmail.ClickAsync();
        if (step >= 2)
        {
            await _wizard.Password.FillAsync(step == 2 ? invalidValue : "SecurePass123");
            await _wizard.ContinueFromPassword.ClickAsync();
        }

        if (step == 3)
        {
            await _wizard.Name.FillAsync(invalidValue);
            await _wizard.CreateAccount.ClickAsync();
        }

        await Expect(_wizard.StepError).ToHaveTextAsync(message);
        await Expect(_wizard.StepIndicator).ToHaveTextAsync($"Step {step} of 3");
        await Expect(Page).ToHaveURLAsync(new Regex("/join$"));
    }

    [Test]
    public async Task Existing_email_is_rejected_by_the_server_and_the_email_is_kept()
    {
        // The browser can't know which emails exist; this rule is checked only on submit.
        await _wizard.GotoAsync();

        await _wizard.CompleteAsync(AdminEmail.ToUpperInvariant(), "SecurePass123", "Copy Cat");

        await Expect(_wizard.ServerError).ToHaveTextAsync("An account with this email already exists.");
        await Expect(Page).ToHaveURLAsync(new Regex("/join$"));
        await Expect(_wizard.Email).ToHaveValueAsync(AdminEmail.ToUpperInvariant());
    }

    [Test]
    public async Task Optional_fields_can_be_filled_and_registration_still_succeeds()
    {
        await _wizard.GotoAsync();
        await _wizard.Email.FillAsync(TimestampedEmail());
        await _wizard.ContinueFromEmail.ClickAsync();
        await _wizard.Password.FillAsync("SecurePass123");
        await _wizard.ContinueFromPassword.ClickAsync();

        await _wizard.Name.FillAsync("Opt Learner");
        await _wizard.DateOfBirth.FillAsync("1990-05-17");           // date inputs take yyyy-MM-dd
        await _wizard.Region.SelectOptionAsync("Europe");            // by visible text or value
        await _wizard.Plan.SelectOptionAsync(new SelectOptionValue { Value = "plus" });
        await _wizard.CreateAccount.ClickAsync();

        await Expect(new DashboardPage(Page).Heading).ToHaveTextAsync("Welcome back, Opt Learner");
    }

    [Test]
    public async Task Pressing_Enter_on_a_step_acts_as_Continue()
    {
        await _wizard.GotoAsync();
        await _wizard.Email.FillAsync(TimestampedEmail());

        await _wizard.Email.PressAsync("Enter");

        await Expect(_wizard.Password).ToBeVisibleAsync();
        await Expect(_wizard.StepError).ToBeHiddenAsync();
    }
}
