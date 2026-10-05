using System.Text.Json.Serialization;
using DocStore.Api.Auth;
using DocStore.Api.Endpoints;
using DocStore.Api.Infrastructure;
using DocStore.Api.Seeding;
using DocStore.Core.Documents;

var builder = WebApplication.CreateBuilder(args);

var uploadOptions = builder.Configuration.GetSection("Upload").Get<UploadOptions>() ?? new UploadOptions();
builder.Services.AddDocStore(builder.Configuration, uploadOptions);
builder.Services.AddDocStoreAuth();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use((context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    return next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.UseApiAntiforgery();

var api = app.MapGroup("/api");
api.MapAuthEndpoints();

var secured = api.MapGroup("").RequireAuthorization();
secured.MapDocumentEndpoints(uploadOptions);
secured.MapUserEndpoints();

await app.InitializeDatabaseAsync();
await app.RunAsync();
