# Todo API

A minimal .NET 10 REST API that keeps a separate todo list for each user, secured with
[ASP.NET Core Identity API endpoints](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)
and bearer tokens. Data lives in an EF Core in-memory database, so it resets on every restart.

## Run

```bash
dotnet run --project TodoApi
```

- Swagger UI: http://localhost:5080/swagger
- OpenAPI document: http://localhost:5080/openapi/v1.json

Both are only mapped in the `Development` environment, which the launch profile sets.
The app's display name comes from the `AppName` setting in `appsettings.json`. It is used in
the OpenAPI title, the Swagger page and the startup messages, and can be overridden with the
`AppName` environment variable.
The API uses port 5080 because macOS AirPlay Receiver already listens on 5000.

## Demo

With the API running, in another terminal:

```bash
./demo.sh                                # or BASE_URL=http://host:port ./demo.sh
```

The script registers two users and logs both in. It then exercises the CRUD endpoints,
validation and refresh tokens, and shows that one user can't reach another user's todos.
Each call checks the expected HTTP status, so the script exits non-zero if anything is wrong.
It requires `curl` and `jq`.

## Endpoints

| Method | Route | Description |
|---|---|---|
| POST | `/auth/register` | Create an account |
| POST | `/auth/login` | Get an access token and a refresh token |
| POST | `/auth/refresh` | Exchange a refresh token for a new access token |
| GET | `/auth/manage/info` | Current account details |
| GET | `/todos` | List your todos |
| GET | `/todos/{id}` | Get one of your todos |
| POST | `/todos` | Create a todo |
| PUT | `/todos/{id}` | Replace one of your todos |
| DELETE | `/todos/{id}` | Delete one of your todos |

`MapIdentityApi` also maps email confirmation, password reset and 2FA endpoints under `/auth`.

Send the access token on every `/todos` call as `Authorization: Bearer <token>`.

## How it works

- **Bearer-only auth.** `AddIdentityCore` and `AddBearerToken` replace the cookie-plus-token
  setup of `AddIdentityApiEndpoints`. `MapIdentityApi` provides the auth endpoints under `/auth`.
- **Per-user isolation.** `AppDbContext` adds a named EF Core query filter
  (`OwnerId == current user`) on `Todo`. It also stamps `OwnerId` on new todos in
  `SaveChangesAsync`. Endpoints never handle owner IDs. Another user's todo simply isn't found
  (404), so callers can't tell which IDs exist.
- **Validation.** `AddValidation()` turns on .NET 10's built-in minimal API validation. It checks
  the DataAnnotations on `TodoRequest`, then its `IValidatableObject.Validate` for rules that
  span several fields (an open todo can't be due in the past). Failures return a 400
  `ValidationProblem` automatically.
- **OpenAPI.** The native `Microsoft.AspNetCore.OpenApi` generator builds the document. XML doc
  comments supply summaries and descriptions. `BearerSecurityTransformer` declares the bearer
  scheme and attaches it, plus a 401 response, only to endpoints that require authorization.
  Swagger UI renders the document.
