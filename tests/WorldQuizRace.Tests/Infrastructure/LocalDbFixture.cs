using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Infrastructure.Persistence;
using WorldQuizRace.Infrastructure.Seed;

namespace WorldQuizRace.Tests.Infrastructure;

/// <summary>
/// Throwaway LocalDB database (unique name): real migrations + seed, dropped after the tests.
/// </summary>
public sealed class LocalDbFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        $"Server=(localdb)\\MSSQLLocalDB;Database=WorldQuizRaceDb_Tests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>Rows written by the first seed.</summary>
    public int FirstSeedRows { get; private set; }

    public WorldQuizRaceDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WorldQuizRaceDbContext>().UseSqlServer(ConnectionString).Options);

    public DatabaseSeeder CreateSeeder(WorldQuizRaceDbContext db) =>
        new(db, Microsoft.Extensions.Options.Options.Create(new GameSettings { CheckpointsPerRoute = 15 }), NullLogger<DatabaseSeeder>.Instance);

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        FirstSeedRows = await CreateSeeder(db).SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class LocalDbCollection : ICollectionFixture<LocalDbFixture>
{
    public const string Name = "LocalDb";
}
