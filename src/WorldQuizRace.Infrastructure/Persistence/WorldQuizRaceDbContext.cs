using Microsoft.EntityFrameworkCore;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence;

/// <summary>Contexte EF Core de l'application. Le mapping est décrit dans Persistence/Configurations.</summary>
public sealed class WorldQuizRaceDbContext(DbContextOptions<WorldQuizRaceDbContext> options) : DbContext(options)
{
    public DbSet<AgeGroup> AgeGroups => Set<AgeGroup>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Challenge> Challenges => Set<Challenge>();

    public DbSet<Route> Routes => Set<Route>();

    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();

    public DbSet<PlayerChallenge> PlayerChallenges => Set<PlayerChallenge>();

    public DbSet<Badge> Badges => Set<Badge>();

    public DbSet<PlayerBadge> PlayerBadges => Set<PlayerBadge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorldQuizRaceDbContext).Assembly);
    }
}
