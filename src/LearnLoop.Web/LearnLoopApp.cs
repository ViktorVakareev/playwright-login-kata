using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearnLoop.Web;

/// <summary>
/// LearnLoop: a small, made-up e-learning site with a sign-in flow to practise UI test automation on.
/// Home, log in, register, a protected dashboard, and sign out. Server-rendered HTML, cookie authentication,
/// anti-forgery tokens on every form, salted password hashes, and a lockout after repeated failures.
/// </summary>
public static class LearnLoopApp
{
    /// <summary>Demo-only credentials for the seeded admin (override with LEARNLOOP_ADMIN_EMAIL / LEARNLOOP_ADMIN_PASSWORD).</summary>
    public static string AdminEmail => Environment.GetEnvironmentVariable("LEARNLOOP_ADMIN_EMAIL") ?? "admin@example.com";
    public static string AdminPassword => Environment.GetEnvironmentVariable("LEARNLOOP_ADMIN_PASSWORD") ?? "admin12345";

    public static WebApplication Create(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();   // keys live in memory: nothing written to disk
        builder.Services.AddSingleton(new UserStore(AdminEmail, AdminPassword));
        builder.Services.AddAntiforgery(o => o.FormFieldName = "__token");
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
        {
            o.LoginPath = "/login";
            o.Cookie.Name = "learnloop.auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
        });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/", (HttpContext ctx, IAntiforgery af) => Page(ctx, af, "LearnLoop", """
            <section class="hero">
              <h1>Learn at your own pace</h1>
              <p>Short courses, practice questions and progress tracking in one place.</p>
              <p><a class="button" href="/register">Create a free account</a></p>
            </section>
            """));

        app.MapGet("/login", (HttpContext ctx, IAntiforgery af, string? returnUrl, string? registered) =>
            LoginPage(ctx, af, returnUrl, error: null, email: "", notice: registered == "1" ? "Account created. Please sign in." : null));

        app.MapPost("/login", async (HttpContext ctx, IAntiforgery af, UserStore users) =>
        {
            await af.ValidateRequestAsync(ctx);
            var form = await ctx.Request.ReadFormAsync();
            var email = form["email"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            var result = users.SignIn(email, form["password"].ToString());
            if (!result.Succeeded)
            {
                ctx.Response.StatusCode = result == LoginResult.LockedOut ? 429 : 401;
                return LoginPage(ctx, af, returnUrl, result.Error, email, notice: null);
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, result.User!.Name), new Claim(ClaimTypes.Email, result.User.Email)],
                CookieAuthenticationDefaults.AuthenticationScheme);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.LocalRedirect(IsLocal(returnUrl) ? returnUrl : "/dashboard");
        });

        app.MapGet("/register", (HttpContext ctx, IAntiforgery af) => RegisterPage(ctx, af, error: null, name: "", email: ""));

        app.MapPost("/register", async (HttpContext ctx, IAntiforgery af, UserStore users) =>
        {
            await af.ValidateRequestAsync(ctx);
            var form = await ctx.Request.ReadFormAsync();
            var name = form["name"].ToString();
            var email = form["email"].ToString();

            if (users.TryRegister(name, email, form["password"].ToString(), out var error))
                return Results.Redirect("/login?registered=1");

            ctx.Response.StatusCode = 400;
            return RegisterPage(ctx, af, error, name, email);
        });

        app.MapGet("/dashboard", (HttpContext ctx, IAntiforgery af) =>
        {
            var name = ctx.User.Identity!.Name ?? "learner";
            return Page(ctx, af, "Dashboard", $"""
                <h1>Welcome back, {Html(name)}</h1>
                <section aria-label="Your progress" class="cards">
                  <article class="card"><h2>Courses in progress</h2><p class="stat">2</p></article>
                  <article class="card"><h2>Completed courses</h2><p class="stat">0</p></article>
                  <article class="card"><h2>Practice sessions</h2><p class="stat">5</p></article>
                </section>
                """);
        }).RequireAuthorization();

        app.MapPost("/logout", async (HttpContext ctx, IAntiforgery af) =>
        {
            await af.ValidateRequestAsync(ctx);
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });

        return app;
    }

    private static IResult LoginPage(HttpContext ctx, IAntiforgery af, string? returnUrl, string? error, string email, string? notice) =>
        Page(ctx, af, "Log in", $"""
            <section class="panel">
              <h1>Welcome back</h1>
              <p>Sign in to continue learning.</p>
              {(notice is null ? "" : $"""<div role="status" class="notice">{Html(notice)}</div>""")}
              {(error is null ? "" : $"""<div role="alert" class="error">{Html(error)}</div>""")}
              <form method="post" action="/login" novalidate>
                {Token(ctx, af)}
                <input type="hidden" name="returnUrl" value="{Html(IsLocal(returnUrl) ? returnUrl! : "")}">
                <label for="email">Email</label>
                <input id="email" name="email" type="email" autocomplete="username" value="{Html(email)}">
                <label for="password">Password</label>
                <input id="password" name="password" type="password" autocomplete="current-password">
                <button type="submit">Sign in</button>
              </form>
              <p>New here? <a href="/register">Create an account</a></p>
            </section>
            """);

    private static IResult RegisterPage(HttpContext ctx, IAntiforgery af, string? error, string name, string email) =>
        Page(ctx, af, "Register", $"""
            <section class="panel">
              <h1>Create your account</h1>
              {(error is null ? "" : $"""<div role="alert" class="error">{Html(error)}</div>""")}
              <form method="post" action="/register" novalidate>
                {Token(ctx, af)}
                <label for="name">Name</label>
                <input id="name" name="name" autocomplete="name" value="{Html(name)}">
                <label for="email">Email</label>
                <input id="email" name="email" type="email" autocomplete="username" value="{Html(email)}">
                <label for="password">Password</label>
                <input id="password" name="password" type="password" autocomplete="new-password">
                <button type="submit">Create account</button>
              </form>
            </section>
            """);

    private static IResult Page(HttpContext ctx, IAntiforgery af, string title, string body)
    {
        var signedIn = ctx.User.Identity?.IsAuthenticated == true;
        var nav = signedIn
            ? $"""<a href="/dashboard">Dashboard</a><form method="post" action="/logout" class="inline">{Token(ctx, af)}<button type="submit" class="link">Sign out</button></form>"""
            : """<a href="/login">Log in</a><a href="/register">Register</a>""";

        return Results.Content($$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{Html(title)}} | LearnLoop</title>
              <style>
                body { font-family: system-ui, sans-serif; margin: 0; background: #f6f7fb; color: #1f2430; }
                header { display: flex; justify-content: space-between; align-items: center; padding: 14px 28px; background: #2d3a8c; }
                header .brand { color: #fff; font-weight: 700; font-size: 20px; text-decoration: none; }
                header nav { display: flex; gap: 18px; align-items: center; }
                header nav a, header nav button.link { color: #fff; text-decoration: none; background: none; border: 0; font: inherit; cursor: pointer; }
                main { max-width: 880px; margin: 40px auto; padding: 0 20px; }
                .panel { max-width: 380px; margin: 0 auto; background: #fff; padding: 28px; border-radius: 10px; box-shadow: 0 2px 10px #0001; }
                label { display: block; margin-top: 14px; font-weight: 600; }
                input { width: 100%; box-sizing: border-box; padding: 9px; margin-top: 6px; border: 1px solid #b9bfd0; border-radius: 6px; font-size: 15px; }
                form button[type=submit]:not(.link), .button { margin-top: 20px; background: #2d3a8c; color: #fff; border: 0; padding: 10px 18px; border-radius: 6px; font-size: 15px; cursor: pointer; text-decoration: none; display: inline-block; }
                .inline { display: inline; margin: 0; } .inline button { margin: 0 !important; }
                .error { background: #fde8e8; color: #9b1c1c; padding: 10px; border-radius: 6px; margin: 12px 0; }
                .notice { background: #e6f4ea; color: #1e6b34; padding: 10px; border-radius: 6px; margin: 12px 0; }
                .cards { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; }
                .card { background: #fff; padding: 18px; border-radius: 10px; box-shadow: 0 2px 10px #0001; }
                .card h2 { font-size: 15px; margin: 0; color: #4a5168; } .stat { font-size: 30px; font-weight: 700; margin: 8px 0 0; }
              </style>
            </head>
            <body>
              <header><a class="brand" href="/">LearnLoop</a><nav aria-label="Main">{{nav}}</nav></header>
              <main>{{body}}</main>
            </body>
            </html>
            """, "text/html; charset=utf-8");
    }

    private static string Token(HttpContext ctx, IAntiforgery af)
    {
        var tokens = af.GetAndStoreTokens(ctx);
        return $"""<input type="hidden" name="{tokens.FormFieldName}" value="{Html(tokens.RequestToken!)}">""";
    }

    private static bool IsLocal(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\");

    private static string Html(string text) => HtmlEncoder.Default.Encode(text);
}
