using Microsoft.EntityFrameworkCore;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Exceptions;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Game;

public interface IMatchResultService
{
    /// <summary>Final statistics of a finished match, with an encouraging message for each player.</summary>
    Task<MatchResultDto> GetResultAsync(int matchId, CancellationToken cancellationToken = default);
}

public sealed class MatchResultService(IApplicationDbContext db) : IMatchResultService
{
    public async Task<MatchResultDto> GetResultAsync(int matchId, CancellationToken cancellationToken = default)
    {
        var match = await db.Matches
                        .AsNoTracking()
                        .Include(m => m.Players).ThenInclude(mp => mp.Player).ThenInclude(p => p.AgeGroup)
                        .Include(m => m.Players).ThenInclude(mp => mp.Route).ThenInclude(r => r.Checkpoints)
                        .SingleOrDefaultAsync(m => m.Id == matchId, cancellationToken)
                    ?? throw new GameNotFoundException(GameErrorCodes.MatchNotFound, "Partie introuvable.");

        if (match.Status != MatchStatus.Finished)
        {
            throw new GameRuleException(GameErrorCodes.MatchNotFinished, "La partie n'est pas encore terminée.");
        }

        var playerIds = match.Players.Select(p => p.PlayerId).ToList();
        var attempts = await db.PlayerChallenges
            .AsNoTracking()
            .Where(pc => pc.MatchId == matchId && pc.Status != AttemptStatus.Pending)
            .Select(pc => new { pc.PlayerId, pc.ServedAt, pc.AnsweredAt })
            .ToListAsync(cancellationToken);
        var badges = await db.PlayerBadges
            .AsNoTracking()
            .Where(pb => playerIds.Contains(pb.PlayerId))
            .OrderBy(pb => pb.AwardedAt)
            .Select(pb => new { pb.PlayerId, Badge = new BadgeDto(pb.Badge.Code, pb.Badge.Name, pb.Badge.Description, pb.Badge.Icon) })
            .ToListAsync(cancellationToken);

        var destination = match.Players.SelectMany(p => p.Route.Checkpoints).FirstOrDefault(c => c.IsFinal)?.Name ?? "la destination";

        var players = match.Players.OrderBy(p => p.Slot).Select(mp =>
        {
            var timeSpent = attempts
                .Where(a => a.PlayerId == mp.PlayerId && a.AnsweredAt is not null)
                .Aggregate(TimeSpan.Zero, (total, a) => total + (a.AnsweredAt!.Value - a.ServedAt));
            var isWinner = match.WinnerId == mp.PlayerId;

            return new PlayerResultDto(
                new PlayerDto(mp.Player.Id, mp.Player.Name, mp.Player.Age, mp.Player.AgeGroupId, mp.Player.AgeGroup.Name, mp.Player.Avatar),
                mp.Slot,
                isWinner,
                mp.CurrentPosition,
                mp.Route.TotalCheckpoints,
                mp.CorrectAnswers,
                mp.WrongAnswers,
                ChallengesAttempted: mp.Attempts,
                ChallengesCompleted: mp.CorrectAnswers,
                Accuracy(mp),
                timeSpent,
                mp.Score,
                mp.Stars,
                mp.Coins,
                badges.Where(b => b.PlayerId == mp.PlayerId).Select(b => b.Badge).ToList(),
                Message(mp, isWinner, destination));
        }).ToList();

        return new MatchResultDto(
            match.Id, match.Status, match.WinnerId, destination, match.StartDate, match.EndDate,
            (match.EndDate ?? match.StartDate) - match.StartDate, players);
    }

    public static double Accuracy(MatchPlayer participant) =>
        participant.Attempts == 0 ? 0 : Math.Round(100.0 * participant.CorrectAnswers / participant.Attempts, 1);

    /// <summary>Child-friendly messages: the winner is congratulated, the other player is always encouraged.</summary>
    public static string Message(MatchPlayer participant, bool isWinner, string destination)
    {
        var name = participant.Player.Name;
        if (isWinner)
        {
            return $"Bravo {name} ! Tu as atteint {destination} en premier. Quelle aventure !";
        }

        var total = participant.Route.TotalCheckpoints;
        return participant.CorrectAnswers == 0
            ? $"Bien essayé {name} ! Chaque défi t'apprend quelque chose : la prochaine course sera la tienne !"
            : $"Super course {name} ! Tu as parcouru {participant.CurrentPosition} étape(s) sur {total} et réussi "
              + $"{participant.CorrectAnswers} défi(s). Continue comme ça, la prochaine victoire t'attend !";
    }
}
