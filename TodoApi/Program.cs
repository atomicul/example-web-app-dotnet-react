using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApi;
using TodoApi.Data;

var builder = WebApplication.CreateBuilder(args);
var appName = builder.Configuration["AppName"] ?? builder.Environment.ApplicationName;

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TodoDb"));

builder.Services.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme);
builder.Services.AddAuthorization();
builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddApiEndpoints();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = appName;
        options.SwaggerEndpoint("/openapi/v1.json", $"{appName} v1");
    });
    app.Lifetime.ApplicationStarted.Register(() =>
        app.Logger.LogInformation("Swagger UI: {SwaggerUrls}", string.Join(", ", app.Urls.Select(url => $"{url}/swagger"))));
}

app.MapGroup("/auth").MapIdentityApi<IdentityUser>().WithTags("Auth");
var v1 = app.MapGroup("/v1");
v1.MapTodos();

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("{AppName} is ready to accept traffic on {Urls}", appName, string.Join(", ", app.Urls)));

app.Run();
