# Postman collection: LearnLoop API Tests

API checks for LearnLoop's JSON API (`src/LearnLoop.Web/Api.cs`), from the local version of the "Validate API Responses" lab in Microsoft's *Automation, Advanced Testing, and AI* course on Coursera.

- **Lab requests (2):** `GET /api/courses` returns 200 with a `courses` array; `POST /api/auth/register` with `{}` returns 400 `validation_error`.
- **Stretch folder (11):** response time, `Content-Type`, JSON schema, paging and its limits, request chaining with a collection variable, a 201 sign-up with a timestamped email, 409 duplicate, 415 non-JSON body, and 404 for an unknown id or route.

13 requests, 29 assertions. The base URL is the collection variable `baseUrl` (default `http://localhost:3000`).

## Run it

Start the app (data is in memory, so every restart gives a clean catalogue):

```powershell
dotnet run --project src/LearnLoop.Web
```

Postman: **Import** → `postman/LearnLoop_API_Tests.postman_collection.json` → collection **⋯** → **Run collection**.

Command line (needs Node.js):

```powershell
npx newman run postman/LearnLoop_API_Tests.postman_collection.json
```

Add `--reporters cli,junit --reporter-junit-export results.xml` for a JUnit report that CI can display.
