using Microsoft.EntityFrameworkCore;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Exceptions;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Game;

public interface IMatchStateQuery
{
    /// <summary>Read-only snapshot of a match: both players, their routes and checkpoints, turn and statistics.</summary>
    Task<MatchStateDto> GetAsync(int matchId, CancellationToken cancellationToken = default);
}

public sealed class MatchStateQuery(IApplicationDbContext db) : IMatchStateQuery
{
    public async Task<MatchStateDto> GetAsync(int matchId, CancellationToken cancellationToken = default)
    {
        var match = await db.Matches
                        .AsNoTracking()
                        .Include(m => m.Players).ThenInclude(mp => mp.Player).ThenInclude(p => p.AgeGroup)
                        .Include(m => m.Players).ThenInclude(mp => mp.Route).ThenInclude(r => r.Checkpoints)
                        .SingleOrDefaultAsync(m => m.Id == matchId, cancellationToken)
                    ?? throw new GameNotFoundException(GameErrorCodes.MatchNotFound, "Partie introuvable.");

        var pendingPlayers = await db.PlayerChallenges
            .Where(pc => pc.MatchId == matchId && pc.Status == AttemptStatus.Pending)
            .Select(pc => pc.PlayerId)
            .ToListAsync(cancellationToken);

        var players = match.Players
            .OrderBy(mp => mp.Slot)
            .Select(mp => new MatchPlayerStateDto(
                new PlayerDto(mp.Player.Id, mp.Player.Name, mp.Player.Age, mp.Player.AgeGroupId, mp.Player.AgeGroup.Name, mp.Player.Avatar),
                mp.Slot,
                new RouteDto(
                    mp.Route.Id, mp.Route.Code, mp.Route.Name, mp.Route.Description, mp.Route.TotalCheckpoints,
                    mp.Route.Checkpoints.OrderBy(c => c.Position)
                        .Select(c => new CheckpointDto(c.Position, c.Name, c.Country, c.FunFact, c.MapX, c.MapY, c.IsFinal))
                        .ToList()),
                mp.CurrentPosition,
                mp.CorrectAnswers,
                mp.WrongAnswers,
                mp.Attempts,
                mp.Score,
                mp.Stars,
                mp.Coins,
                IsCurrentTurn: match.CurrentTurnPlayerId == mp.PlayerId,
                HasPendingChallenge: pendingPlayers.Contains(mp.PlayerId)))
            .ToList();

        var current = match.Players.FirstOrDefault(mp => mp.PlayerId == match.CurrentTurnPlayerId);
        return new MatchStateDto(
            match.Id,
            match.Status,
            match.TurnMode,
            match.TurnNumber,
            match.CurrentTurnPlayerId,
            current?.Slot,
            players.Max(p => p.Route.TotalCheckpoints),
            match.WinnerId,
            match.StartDate,
            match.EndDate,
            players);
    }
}
