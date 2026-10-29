using LearnLoop.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace LearnLoop.UiTests;   // root namespace on purpose: a [SetUpFixture] only runs for tests in its own namespace and below

/// <summary>
/// Starts the LearnLoop demo app once for the whole test run, on a free local port.
/// No separate "start the app first" step, and no clash with anything already on port 3000.
/// </summary>
[SetUpFixture]
public sealed class AppServer
{
    private static WebApplication? _app;

    public static string BaseUrl { get; private set; } = "";

    [OneTimeSetUp]
    public async Task StartApp()
    {
        _app = LearnLoopApp.Create([]);
        _app.Urls.Add("http://127.0.0.1:0");   // port 0 = let the OS pick a free port
        await _app.StartAsync();
        BaseUrl = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
    }

    [OneTimeTearDown]
    public async Task StopApp()
    {
        if (_app is null) return;
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
