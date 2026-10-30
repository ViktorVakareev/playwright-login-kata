using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace LearnLoop.Web;

/// <summary>
/// A small JSON API under /api, for API-testing practice (Postman, Playwright APIRequest, HttpClient).
/// No authentication; every error has the same shape: { "error": { "code", "message", "fields"? } }.
///
///   GET  /api/courses            200 { courses: [...], pagination: {...} }   (?page, ?pageSize 1..50, ?subject)
///   GET  /api/courses/{id}       200 { course } | 404 not_found
///   POST /api/auth/register      201 { user } | 400 validation_error / invalid_json | 409 email_taken | 415 unsupported_media_type
///   anything else under /api     404 route_not_found
/// </summary>
public static class LearnLoopApi
{
    public sealed record Course(string Id, string Title, string Subject, string Level, decimal Price, int DurationHours, double Rating);

    public static readonly IReadOnlyList<Course> Courses =
    [
        new("crs_01", "Software Testing Fundamentals", "Software Testing", "beginner", 0m, 6, 4.6),
        new("crs_02", "Test Design Techniques in Practice", "Software Testing", "intermediate", 31.69m, 9, 4.5),
        new("crs_03", "Exploratory Testing Sessions", "Software Testing", "intermediate", 24.00m, 5, 4.3),
        new("crs_04", "API Testing with Postman", "API Testing", "beginner", 19.99m, 7, 4.7),
        new("crs_05", "Contract Testing for Microservices", "API Testing", "advanced", 49.00m, 8, 4.4),
        new("crs_06", "REST Status Codes Explained", "API Testing", "beginner", 0m, 2, 4.2),
        new("crs_07", "UI Automation with Playwright", "Test Automation", "intermediate", 39.50m, 12, 4.8),
        new("crs_08", "Page Objects That Last", "Test Automation", "intermediate", 29.00m, 6, 4.5),
        new("crs_09", "Flaky Tests: Find and Fix", "Test Automation", "advanced", 44.90m, 7, 4.6),
        new("crs_10", "CI Pipelines for Testers", "DevOps", "intermediate", 34.00m, 10, 4.4),
        new("crs_11", "Docker for QA Engineers", "DevOps", "beginner", 22.50m, 8, 4.3),
        new("crs_12", "Kubernetes Basics", "DevOps", "intermediate", 45.00m, 14, 4.1),
        new("crs_13", "SQL for Testers", "Data", "beginner", 15.00m, 6, 4.5),
        new("crs_14", "Test Data Management", "Data", "intermediate", 27.75m, 5, 4.0),
        new("crs_15", "C# for Test Automation", "Programming", "beginner", 25.00m, 15, 4.6),
        new("crs_16", "Python for Testers", "Programming", "beginner", 25.00m, 15, 4.7),
        new("crs_17", "Performance Testing with k6", "Performance", "intermediate", 38.00m, 9, 4.4),
        new("crs_18", "Accessibility Testing Essentials", "Accessibility", "beginner", 18.00m, 4, 4.5),
        new("crs_19", "Security Testing for QA", "Security", "advanced", 52.00m, 11, 4.3),
        new("crs_20", "Evaluating LLM Applications", "AI", "advanced", 59.00m, 10, 4.8),
    ];

    public static WebApplication MapLearnLoopApi(this WebApplication app)
    {
        app.MapGet("/api/courses", (HttpContext ctx) =>
        {
            var problems = new List<FieldError>();
            var page = ReadInt(ctx, "page", 1, 1, int.MaxValue, problems);
            var pageSize = ReadInt(ctx, "pageSize", 20, 1, 50, problems);
            if (problems.Count > 0) return ValidationError(problems);

            string? subject = ctx.Request.Query["subject"];
            var matching = string.IsNullOrWhiteSpace(subject)
                ? Courses
                : Courses.Where(c => c.Subject.Equals(subject.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

            var total = matching.Count;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var items = matching.Skip((page - 1) * pageSize).Take(pageSize);
            return Results.Json(new { courses = items, pagination = new { page, pageSize, total, totalPages } });
        });

        app.MapGet("/api/courses/{id}", (string id) =>
            Courses.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) is { } course
                ? Results.Json(new { course })
                : Error(404, "not_found", $"No course with id '{id}'."));

        app.MapPost("/api/auth/register", async (HttpContext ctx, UserStore users) =>
        {
            if (!ctx.Request.HasJsonContentType())
                return Error(415, "unsupported_media_type", "Send the body as JSON (Content-Type: application/json).");

            JsonElement body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
            }
            catch (JsonException)
            {
                return Error(400, "invalid_json", "The request body is not valid JSON.");
            }

            if (body.ValueKind != JsonValueKind.Object)
                return Error(400, "invalid_json", "The request body must be a JSON object.");

            var problems = new List<FieldError>();
            var email = ReadString(body, "email", problems);
            var password = ReadString(body, "password", problems, trim: false);
            var name = ReadString(body, "name", problems);

            if (email is not null && (!email.Contains('@') || email.Contains(' ') || email.StartsWith('@') || email.EndsWith('@')))
                problems.Add(new("email", "Must be a valid email address"));
            if (password is not null && password.Length < 8)
                problems.Add(new("password", "Must be at least 8 characters"));
            if (name is not null && name.Length < 2)
                problems.Add(new("name", "Must be at least 2 characters"));

            if (problems.Count > 0) return ValidationError(Ordered(problems));

            if (!users.TryRegister(name!, email!, password!, out var error))
                return Error(409, "email_taken", error ?? "An account with this email already exists.");

            return Results.Json(new { user = new { name, email } }, statusCode: StatusCodes.Status201Created);
        });

        // Anything else under /api gets a JSON 404 instead of an empty response or an HTML page.
        app.Map("/api/{**rest}", () => Error(404, "route_not_found", "Route not found."));

        return app;
    }

    public sealed record FieldError(string Field, string Message);

    private static string? ReadString(JsonElement body, string field, List<FieldError> problems, bool trim = true)
    {
        if (!body.TryGetProperty(field, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            problems.Add(new(field, "Required"));
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            problems.Add(new(field, "Must be a string"));
            return null;
        }

        var text = value.GetString()!;
        if (string.IsNullOrWhiteSpace(text))
        {
            problems.Add(new(field, "Required"));
            return null;
        }

        return trim ? text.Trim() : text;
    }

    private static int ReadInt(HttpContext ctx, string name, int fallback, int min, int max, List<FieldError> problems)
    {
        string? raw = ctx.Request.Query[name];
        if (string.IsNullOrEmpty(raw)) return fallback;
        if (int.TryParse(raw, out var value) && value >= min && value <= max) return value;
        problems.Add(new(name, $"Must be a whole number from {min} to {(max == int.MaxValue ? "any" : max)}"));
        return fallback;
    }

    // Report fields in a stable order (email, password, name) so tests can rely on it.
    private static List<FieldError> Ordered(List<FieldError> problems)
    {
        string[] order = ["email", "password", "name"];
        return problems.OrderBy(p => Array.IndexOf(order, p.Field)).ToList();
    }

    private static IResult ValidationError(List<FieldError> fields) =>
        Results.Json(new { error = new { code = "validation_error", message = "Request validation failed.", fields } }, statusCode: 400);

    private static IResult Error(int status, string code, string message) =>
        Results.Json(new { error = new { code, message } }, statusCode: status);
}
