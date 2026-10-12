using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TodoApi;
using TodoApi.Auth;
using TodoApi.Todos;
using TodoApi.Data;

var builder = WebApplication.CreateBuilder(args);
var appName = builder.Configuration["AppName"] ?? builder.Environment.ApplicationName;

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TodoDb"));

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddTransient<IEmailSender<IdentityUser>, LoggingEmailSender>();

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = appName;
        document.Info.Description = "Per-user todo lists secured with ASP.NET Core Identity bearer tokens. "
            + "Register and log in through the Auth endpoints, then authorize with the returned access token.";
        return Task.CompletedTask;
    });
    options.AddDocumentTransformer<BearerSecurityTransformer>();
    options.AddOperationTransformer<BearerSecurityTransformer>();
});

var app = builder.Build();

void OnServerStarted(Action<ICollection<string>> log) =>
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        if (app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses is { } urls)
        {
            log(urls);
        }
    });

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = appName;
        options.SwaggerEndpoint("/openapi/v1.json", $"{appName} v1");
    });
    OnServerStarted(urls =>
        app.Logger.LogInformation("Swagger UI: {SwaggerUrls}", string.Join(", ", urls.Select(url => $"{url}/swagger"))));
}

app.UseStaticFiles();

var api = app.MapGroup("/api");
api.MapAuth();
api.MapGroup("/v1").MapTodos();
api.MapFallback(() => TypedResults.NotFound());
app.MapFallbackToFile("index.html");

OnServerStarted(urls =>
    app.Logger.LogInformation("{AppName} is ready to accept traffic on {Urls}", appName, string.Join(", ", urls)));

app.Run();
