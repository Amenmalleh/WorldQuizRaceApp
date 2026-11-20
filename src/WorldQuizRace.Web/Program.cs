using System.Text.Json.Serialization;
using WorldQuizRace.Application;
using WorldQuizRace.Infrastructure;
using WorldQuizRace.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(json => json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

// The game API is called by same-origin JavaScript: the token travels in a header, the cookie is SameSite=Strict.
builder.Services.AddAntiforgery(antiforgery =>
{
    antiforgery.HeaderName = "X-CSRF-TOKEN";
    antiforgery.Cookie.Name = "WorldQuizRace.Antiforgery";
    antiforgery.Cookie.SameSite = SameSiteMode.Strict;
    antiforgery.Cookie.HttpOnly = true;
});

var app = builder.Build();

// Migrations + seed data at startup (can be disabled with "Database:InitializeOnStartup": false).
if (app.Configuration.GetValue("Database:InitializeOnStartup", defaultValue: true))
{
    await app.Services.InitializeDatabaseAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();

/// <summary>Exposed for integration tests (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
