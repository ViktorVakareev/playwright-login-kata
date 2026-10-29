// Run the demo app on its own:  dotnet run --project src/LearnLoop.Web   → http://localhost:3000
// Then record against it:      pwsh tests/LearnLoop.UiTests/bin/Debug/net10.0/playwright.ps1 codegen --target csharp-nunit http://localhost:3000
var app = LearnLoop.Web.LearnLoopApp.Create(args);
app.Run(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is { Length: > 0 } ? null : "http://localhost:3000");
