using Microsoft.EntityFrameworkCore;

namespace WorldQuizRace.Infrastructure.Persistence;

/// <summary>Contexte EF Core de l'application. Les DbSet et configurations arrivent en phase 2 (données).</summary>
public sealed class WorldQuizRaceDbContext(DbContextOptions<WorldQuizRaceDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorldQuizRaceDbContext).Assembly);
    }
}
