# Playwright for .NET: what goes where, and what to learn next

The quickest way to learn Playwright's syntax is to learn its object model, then extend this kata, whose every line you already understand.

## 1. The object model: what lives where, and why

```
Playwright        the driver process (started once)
 └─ Browser       one Chromium/Firefox/WebKit instance (expensive, shared)
     └─ Context   an isolated "incognito profile": cookies, storage, permissions (cheap, one per test)
         └─ Page  one tab: navigation, events, dialogs
             └─ Locator  a *description* of how to find an element, evaluated lazily, retried
                 └─ Expect(...)  web-first assertions that wait and retry until true or timeout
```

**Why it's built this way:** a browser is slow to start, so it's shared. *A context is cheap and fully isolated, so each test gets a fresh one. That's why tests can run in parallel without logging each other out, which was the hard part with Selenium.*

**Where each one lives in this kata:**

| Concept | Where | Why there |
|---|---|---|
| `Browser` | Created by `PageTest` (Microsoft.Playwright.NUnit) | One per worker; you never manage it |
| `Context` + `ContextOptions()` | `UiTest.ContextOptions()` sets `BaseURL` | Every test's context gets the same base URL, so tests use `"/login"` |
| `Context.Tracing` | `UiTest` `[SetUp]` / `[TearDown]` | Cross-cutting concern: belongs in the base class, not in tests |
| `Page` | `PageTest.Page`, passed into page objects | Page objects wrap a page; tests don't touch selectors |
| `Locator` | Properties in `Pages.cs` (`Email => page.GetByLabel("Email")`) | Defined once, re-evaluated on every use, so never stale (no `StaleElementReferenceException`) |
| Actions (`FillAsync`, `ClickAsync`) | Page object methods (`SignInAsync`) | Business-level steps, reusable |
| `Expect(...)` | In the **tests** | Assertions are the test's job; page objects stay neutral |
| Browser choice, headless, timeouts | `playwright.runsettings` | Configuration, not code; overridable from the command line |
| App startup | `AppServer` `[SetUpFixture]` | Once per run, shared by all tests (in the root namespace, or it won't run for them) |

## 2. Syntax worth knowing cold

**Locators, best first:**
```csharp
Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });   // how users and screen readers see it
Page.GetByLabel("Email");                                       // form fields
Page.GetByText("Welcome back", new() { Exact = true });         // visible text
Page.GetByTestId("submit");                                     // data-testid, when you control the markup
Page.Locator("css=.card >> nth=0");                             // CSS/XPath only as a last resort

// Narrowing and filtering
Page.GetByRole(AriaRole.Article).Filter(new() { HasText = "Completed" }).GetByRole(AriaRole.Paragraph);
Page.Locator(".card").Nth(1);  .First;  .Last;  await locator.CountAsync();
```

**Actions** (each one auto-waits until the element is visible, enabled and stable):
`ClickAsync`, `FillAsync`, `PressAsync("Enter")`, `CheckAsync`, `SelectOptionAsync`, `HoverAsync`, `SetInputFilesAsync`, `DragToAsync`

**Web-first assertions** (they retry until the timeout; never use `Assert.That(await x.IsVisibleAsync())`):
```csharp
await Expect(locator).ToBeVisibleAsync();      .ToHaveTextAsync("...");     .ToContainTextAsync("...");
await Expect(locator).ToHaveValueAsync("");    .ToHaveAttributeAsync("type", "password");  .ToHaveCountAsync(3);
await Expect(Page).ToHaveURLAsync(new Regex("/dashboard$"));   .ToHaveTitleAsync("...");
await Expect(locator).Not.ToBeVisibleAsync();
```

**Network, the feature Selenium doesn't have:**
```csharp
await Page.RouteAsync("**/api/courses", r => r.FulfillAsync(new() { Status = 500 }));     // fake a server error
var response = await Page.RunAndWaitForResponseAsync(() => button.ClickAsync(), "**/login");
var api = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = url });            // API calls without a browser
```

## 3. Practice plan: extend this kata (about 6–8 evenings)

| # | Exercise | What you'll learn |
|---|---|---|
| 1 | Run with `PWDEBUG=1`, step through one test in the Inspector; then open a failure trace with `show-trace` | Debugging tools: where time goes, what the DOM looked like |
| 2 | Record a registration with Codegen, then refactor it into `RegisterPage` the way the login was done | Recording vs maintainable code |
| 3 | Dashboard cards: assert there are 3 cards and that "Completed courses" shows 0, using `Filter(HasText)` and chained locators | Scoping locators, lists, `ToHaveCountAsync` |
| 4 | Log in **once per run** in the `[SetUpFixture]`, save storage state, and make `DashboardTests` start already signed in (`ContextOptions` with `StorageStatePath`) | Fast suites: the auth pattern real teams use |
| 5 | Add a `/api/me` endpoint to LearnLoop; test it with `Playwright.APIRequest`, then create a user through the API and log in through the UI | API and UI in one test |
| 6 | Use `RouteAsync` to make a page's API call fail or run slowly, and assert the UI shows an error or a loading state | Network control: error and edge-state testing |
| 7 | Run the suite on Firefox and WebKit (`-- Playwright.BrowserName=firefox`), plus a mobile viewport | Cross-browser and responsive testing |
| 8 | Add an accessibility check (`Deque.AxeCore.Playwright`) on the login page | Accessibility testing |

**Reference:** [playwright.dev/dotnet](https://playwright.dev/dotnet/docs/intro). The most useful pages are *Locators*, *Assertions*, *Authentication*, *Network*, *Trace viewer* and *Test runners (NUnit)*.

**In the course:** the next modules (refining scripts with CSS and text locators and DevTools, then version control and pull requests) fit well with exercises 1–3.
