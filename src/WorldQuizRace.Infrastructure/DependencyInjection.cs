using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Infrastructure.Persistence;
using WorldQuizRace.Infrastructure.Seed;

namespace WorldQuizRace.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "WorldQuizRaceDb";

    /// <summary>Registers persistence (EF Core / SQL Server) and infrastructure services.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<WorldQuizRaceDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(WorldQuizRaceDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<WorldQuizRaceDbContext>());
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
