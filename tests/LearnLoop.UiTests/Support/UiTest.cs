using LearnLoop.Web;
using Microsoft.Playwright.NUnit;
using NUnit.Framework.Interfaces;

namespace LearnLoop.UiTests.Support;

/// <summary>
/// Base class for UI tests: a fresh browser context per test, pointed at the running app,
/// with a Playwright trace saved only when the test fails.
/// </summary>
public abstract class UiTest : PageTest
{
    protected static string AdminEmail => LearnLoopApp.AdminEmail;
    protected static string AdminPassword => LearnLoopApp.AdminPassword;

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = AppServer.BaseUrl };

    /// <summary>A unique address per call, so registration tests never collide.</summary>
    protected static string UniqueEmail(string prefix = "learner") => $"{prefix}.{Guid.NewGuid():N}@example.com";

    [SetUp]
    public async Task StartTracing() =>
        await Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

    [TearDown]
    public async Task SaveTraceOnFailure()
    {
        var failed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;
        if (!failed)
        {
            await Context.Tracing.StopAsync();
            return;
        }

        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces", $"{TestContext.CurrentContext.Test.Name}.zip");
        await Context.Tracing.StopAsync(new() { Path = path });
        TestContext.AddTestAttachment(path, "Open with: pwsh bin/Debug/net10.0/playwright.ps1 show-trace <file>");
    }
}
