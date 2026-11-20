using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Exceptions;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Matches;

public interface IMatchService
{
    /// <summary>
    /// Creates an in-progress match: player A on the west route, player B on the east route, both at position 0,
    /// the first player chosen according to GameSettings. Returns the new match id.
    /// </summary>
    Task<int> CreateMatchAsync(int playerAId, int playerBId, CancellationToken cancellationToken = default);
}

public sealed class MatchService(
    IApplicationDbContext db,
    IOptions<GameSettings> options,
    IRandomProvider random,
    TimeProvider time) : IMatchService
{
    public async Task<int> CreateMatchAsync(int playerAId, int playerBId, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (playerAId == playerBId)
        {
            throw new GameValidationException("playerB", "Les deux joueurs doivent être différents.");
        }

        var players = await db.Players.Where(p => p.Id == playerAId || p.Id == playerBId).ToListAsync(cancellationToken);
        if (players.Count != 2)
        {
            throw new GameNotFoundException(GameErrorCodes.PlayerNotFound, "Joueur introuvable.");
        }

        var busy = await db.MatchPlayers.AnyAsync(
            mp => (mp.PlayerId == playerAId || mp.PlayerId == playerBId) && mp.Match.Status == MatchStatus.InProgress,
            cancellationToken);
        if (busy)
        {
            throw new GameRuleException(GameErrorCodes.PlayerAlreadyInMatch, "Un des joueurs participe déjà à une partie en cours.");
        }

        var routeA = await GetRouteAsync(settings.RouteForPlayerA, settings.CheckpointsPerRoute, cancellationToken);
        var routeB = await GetRouteAsync(settings.RouteForPlayerB, settings.CheckpointsPerRoute, cancellationToken);

        var firstSlot = settings.FirstPlayer switch
        {
            FirstPlayerOption.A => PlayerSlot.A,
            FirstPlayerOption.B => PlayerSlot.B,
            _ => random.Next(2) == 0 ? PlayerSlot.A : PlayerSlot.B
        };

        var match = new Match
        {
            TurnMode = settings.TurnMode,
            Status = MatchStatus.InProgress,
            StartDate = time.GetUtcNow(),
            TurnNumber = 1,
            CurrentTurnPlayerId = firstSlot == PlayerSlot.A ? playerAId : playerBId
        };
        match.Players.Add(new MatchPlayer { PlayerId = playerAId, Slot = PlayerSlot.A, RouteId = routeA.Id });
        match.Players.Add(new MatchPlayer { PlayerId = playerBId, Slot = PlayerSlot.B, RouteId = routeB.Id });

        db.Matches.Add(match);
        await db.SaveChangesAsync(cancellationToken);
        return match.Id;
    }

    /// <summary>Both routes must exist, be active and have exactly the configured number of checkpoints (fairness).</summary>
    private async Task<Route> GetRouteAsync(string code, int expectedCheckpoints, CancellationToken cancellationToken)
    {
        var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(r => r.Code == code && r.IsActive, cancellationToken);
        var checkpoints = route is null ? 0 : await db.Checkpoints.CountAsync(c => c.RouteId == route.Id, cancellationToken);
        if (route is null || route.TotalCheckpoints != expectedCheckpoints || checkpoints != expectedCheckpoints)
        {
            throw new GameRuleException(GameErrorCodes.RouteUnavailable, $"La route {code} n'est pas disponible.");
        }

        return route;
    }
}
