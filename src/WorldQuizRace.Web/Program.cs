using WorldQuizRace.Application;
using WorldQuizRace.Infrastructure;
using WorldQuizRace.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Migrations + données initiales au démarrage (désactivable : "Database:InitializeOnStartup": false).
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

/// <summary>Exposé pour les tests d'intégration (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
