using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Application.Abstractions;

/// <summary>
/// Persistence abstraction used by the application services. Implemented by the Infrastructure DbContext,
/// so the Application layer depends on EF Core abstractions only, never on SQL Server.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<AgeGroup> AgeGroups { get; }

    DbSet<Player> Players { get; }

    DbSet<Category> Categories { get; }

    DbSet<Challenge> Challenges { get; }

    DbSet<Route> Routes { get; }

    DbSet<Checkpoint> Checkpoints { get; }

    DbSet<Match> Matches { get; }

    DbSet<MatchPlayer> MatchPlayers { get; }

    DbSet<PlayerChallenge> PlayerChallenges { get; }

    DbSet<Badge> Badges { get; }

    DbSet<PlayerBadge> PlayerBadges { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
