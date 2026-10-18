# Backend

The .NET solution: `TodoApi`, the ASP.NET Core minimal API, and `TodoApi.Tests`, its integration tests. The
[root README](../README.md) covers running, configuring and deploying the app; this one covers the code patterns to
follow when changing the API.

## Endpoints

Each feature has a folder, such as `Todos/`, holding its entities, its contracts and a `MapX` extension that maps a
tagged group of handlers (`TodoEndpoints.MapTodos`). `Program.cs` maps every feature under the `/api` group.

Handlers are static methods that return `TypedResults`, or a `Results<…>` union when they can answer in several ways,
so the OpenAPI document lists every response without extra attributes. Their XML doc comments, and those of the
contracts' records, become the summaries and descriptions in the document:

```csharp
/// <summary>Gets one of the current user's todos.</summary>
/// <param name="id">Identifier of the todo.</param>
public static async Task<Results<Ok<TodoResponse>, NotFound>> GetById(int id, AppDbContext db, CancellationToken ct)
```

Identity's own endpoints come from `MapIdentityApi`, under `/api/auth`. `Auth/AuthEndpoints.cs` adjusts them with
conventions that pick routes by method and path, rather than writing them anew: the stricter rate limit of those that
send an email, and the filters in `Auth/AccountChanges.cs`: around `POST /manage/info`, which checks a new email and
remembers it until it is confirmed, and around `GET /confirmEmail`, which only confirms the email remembered.
Endpoints Identity lacks, such as `GET` and `DELETE /manage/pendingEmail`, are mapped next to them.

## Validation errors

A refused request gets ASP.NET's own validation problem, whose `errors` map reads as the framework writes it, plus an
`issues` member that lists each error as an [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) error object: a JSON
Pointer to the request field it is about, what is wrong in English, and a code clients word it by in their own
language.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "DueDate": ["An open todo can't be due in the past."] },
  "issues": [
    { "pointer": "#/dueDate", "detail": "An open todo can't be due in the past.", "code": "DueDateInPast" }
  ]
}
```

`Validation/ValidationProblems.cs` builds every such problem, so a rule only declares its code and message where it
fails:

- **Rules on a property** are validation attributes on the contract, e.g. `[property: Required, StringLength(200)]`.
  They are also written into the OpenAPI document, which is where the SPA's checks come from. An attribute's error is
  coded by the attribute's name, such as `Required` or `StringLength`.
- **Rules across properties** go in the contract's `IValidatableObject.Validate`, which returns a
  `CodedValidationResult` naming its code:

  ```csharp
  if (!IsComplete && DueDate < DateOnly.FromDateTime(DateTime.UtcNow))
  {
      yield return new CodedValidationResult("DueDateInPast", "An open todo can't be due in the past.", [nameof(DueDate)]);
  }
  ```

  They run only once every attribute passes, as with ASP.NET's own validation.
- **Rules that need the database** run in the handler, after it loads what it needs, and answer with
  `ValidationProblems.For([new ValidationError(nameof(TodoRequest.Title), "DuplicateTitle", "…")])`. Add
  `ValidationProblem` to the handler's `Results<…>`. When the database can enforce the rule, as a unique index does,
  keep the constraint too, so concurrent requests can't both pass the check, and answer its `DbUpdateException` with
  the same error.
- **Identity's errors** keep its own codes, such as `PasswordTooShort` or `DuplicateUserName`, which Identity uses as
  their keys. They are not about one field of the request, so their issues have no pointer.

The endpoint filter that runs the rules, `ValidationProblems.ValidateRequests`, replaces ASP.NET's `AddValidation`,
whose results carry only messages. It validates every handler argument whose type has rules, but only that argument's
own properties: an object or collection nested in a contract is not validated. Contracts are flat today; extend the
filter before nesting one.

## The OpenAPI document

The OpenAPI document is the SPA's contract: `make api` generates the SPA's client, its types and its zod checks from
it, so run it after changing an endpoint or a contract. When something the API enforces isn't in the document by
default, a transformer writes it there:

- `Auth/CredentialRulesSchemaTransformer.cs` writes the email and password rules of Identity's `IdentityOptions` into
  the schemas of the requests that set them, `RegisterRequest` and `InfoRequest`.
- `Validation/ValidationIssuesSchemaTransformer.cs` documents the `issues` member of validation problems.
- `BearerSecurityTransformer.cs` adds the bearer scheme and marks the operations that require it.

## Per-user data

An entity that belongs to a user implements `Data/IOwnedByUser`. `AppDbContext` then links it to its owner, filters
every query to the signed-in user and stamps the owner on new rows. Handlers never filter or assign by user, and one
user's ids are simply not found for another:

```csharp
var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
return todo is null ? TypedResults.NotFound() : TypedResults.Ok(TodoResponse.From(todo));
```

## Cloud services

The root README's [Cloud services](../README.md#cloud-services) section sets the rules; in code, each feature that
uses one looks like the emails:

- The feature's own interface is the swap point: Identity's `IEmailSender<IdentityUser>`.
- A stand-in implementation needs nothing but the API (`LoggingEmailSender`), and a Dapr one uses `DaprClient`
  directly (`DaprEmailSender`).
- One registration method picks between them by a setting, never by environment (`Emails.AddEmails`, by
  `Email:Queue`). Behaviour every transport shares wraps them instead of being repeated: `SpaLinkEmailSender` points
  links at the SPA and hands the email to the keyed transport.

## Tests

`TodoApi.Tests` boots the whole API with `WebApplicationFactory` against SQL Server in a Docker container that
Testcontainers starts and removes for each run, so the tests also apply the real migrations. Every test registers its
own user, and all data belongs to a user, so tests share the database without resetting it. Identity's emails are
captured instead of logged, which lets tests follow confirmation links. The tests of what the API sends through Dapr
run a Dapr sidecar in another container, whose `bindings.localstorage` bindings write every message they get to a
temporary directory for the tests to read. Run them alone with `dotnet test --solution backend/TodoApp.slnx`.

Tests act as a client would, through the helpers in `TestUsers` and `TodoClient`. A refused request is read with
`ReadValidationProblemAsync`, which asserts the 400 and returns both the problem as ASP.NET's clients read it and its
issues, so a test can check the field, the code or both.
