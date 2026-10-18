using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json.Serialization;
using TodoApi;
using TodoApi.Auth;
using TodoApi.Todos;
using TodoApi.Data;
using TodoApi.Validation;

var builder = WebApplication.CreateBuilder(args);
var appName = builder.Configuration["AppName"] ?? builder.Environment.ApplicationName;
var isGeneratingOpenApiDocument = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
if (!isGeneratingOpenApiDocument)
{
    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<AppDbContext>()
        .ProtectKeysWithConfiguredCertificate(builder.Configuration);
}

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options => options.SignIn.RequireConfirmedEmail = true)
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddDaprSidecar(builder.Configuration);
builder.Services.AddEmails(builder.Configuration);
builder.Services.AddTrustedProxies(builder.Configuration);
builder.Services.AddRateLimits(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = appName;
        document.Info.Description = builder.Configuration["AppDescription"];
        return Task.CompletedTask;
    });
    options.AddDocumentTransformer<BearerSecurityTransformer>();
    options.AddOperationTransformer<BearerSecurityTransformer>();
    options.AddSchemaTransformer<CredentialRulesSchemaTransformer>();
    options.AddSchemaTransformer<ValidationIssuesSchemaTransformer>();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Configuration.GetSection("Kestrel:Endpoints:Https").Exists())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    if (!isGeneratingOpenApiDocument)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }

    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = appName;
        options.SwaggerEndpoint("/openapi/v1.json", $"{appName} v1");
    });
    app.Lifetime.ApplicationStarted.Register(() =>
        app.Logger.LogInformation("Swagger UI: {SwaggerUrls}", string.Join(", ", app.Urls.Select(url => $"{url}/swagger"))));
}

app.UseStaticFiles();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapAcmeChallenge();

var api = app.MapGroup("/api").RequireRateLimiting(RateLimits.Api);
api.AddEndpointFilterFactory(ValidationProblems.ValidateRequests);
api.MapAuth();
api.MapTodos();
api.MapFallback(() => TypedResults.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
