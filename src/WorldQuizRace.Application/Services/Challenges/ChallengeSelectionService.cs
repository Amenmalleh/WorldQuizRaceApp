using Microsoft.EntityFrameworkCore;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Exceptions;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Challenges;

public enum ChallengeSelectionKind
{
    /// <summary>Unseen challenge of the target difficulty (normal case).</summary>
    Fresh = 1,

    /// <summary>Target difficulty exhausted: unseen challenge of the closest difficulty.</summary>
    NeighborDifficulty = 2,

    /// <summary>Every active challenge of the age group was already served: least recently seen one.</summary>
    LeastRecentlySeen = 3
}

public sealed record ChallengeSelection(Challenge Challenge, Difficulty TargetDifficulty, ChallengeSelectionKind Kind);

public interface IChallengeSelectionService
{
    Task<ChallengeSelection> SelectAsync(int matchId, int playerId, int ageGroupId, int targetPosition, CancellationToken cancellationToken = default);
}

/// <summary>
/// Picks a random active challenge for the player's age group and the difficulty of the position being attempted,
/// excluding challenges this player already saw in this match.
/// Fallback when a difficulty is exhausted (only 15 challenges per difficulty):
/// 1) an unseen challenge of the closest difficulty (easier first on a tie: Medium → Easy → Hard);
/// 2) when the whole age group is exhausted, the least recently seen challenge, preferring the target difficulty.
/// </summary>
public sealed class ChallengeSelectionService(IApplicationDbContext db, IDifficultyPolicy difficulties, IRandomProvider random)
    : IChallengeSelectionService
{
    public async Task<ChallengeSelection> SelectAsync(int matchId, int playerId, int ageGroupId, int targetPosition, CancellationToken cancellationToken = default)
    {
        var target = difficulties.ForPosition(targetPosition);

        var pool = await db.Challenges
            .AsNoTracking()
            .Where(c => c.AgeGroupId == ageGroupId && c.IsActive)
            .Select(c => new { c.Id, c.Difficulty })
            .ToListAsync(cancellationToken);
        if (pool.Count == 0)
        {
            throw new GameRuleException(GameErrorCodes.NoChallengeAvailable, "Aucun défi n'est disponible pour cette tranche d'âge.");
        }

        var lastSeen = await db.PlayerChallenges
            .AsNoTracking()
            .Where(pc => pc.MatchId == matchId && pc.PlayerId == playerId)
            .GroupBy(pc => pc.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, LastServedAt = g.Max(pc => pc.ServedAt) })
            .ToDictionaryAsync(x => x.ChallengeId, x => x.LastServedAt, cancellationToken);

        int chosenId = 0;
        var kind = ChallengeSelectionKind.LeastRecentlySeen;
        foreach (var difficulty in difficulties.FallbackOrder(target))
        {
            var unseen = pool.Where(c => c.Difficulty == difficulty && !lastSeen.ContainsKey(c.Id)).Select(c => c.Id).ToList();
            if (unseen.Count > 0)
            {
                chosenId = unseen[random.Next(unseen.Count)];
                kind = difficulty == target ? ChallengeSelectionKind.Fresh : ChallengeSelectionKind.NeighborDifficulty;
                break;
            }
        }

        if (chosenId == 0)
        {
            // Everything was seen: replay the challenge seen the longest time ago, closest difficulty first.
            var order = difficulties.FallbackOrder(target).Select((d, rank) => (d, rank)).ToDictionary(x => x.d, x => x.rank);
            chosenId = pool
                .OrderBy(c => order[c.Difficulty])
                .ThenBy(c => lastSeen[c.Id])
                .ThenBy(c => c.Id)
                .First().Id;
        }

        var challenge = await db.Challenges.Include(c => c.Category).SingleAsync(c => c.Id == chosenId, cancellationToken);
        return new ChallengeSelection(challenge, target, kind);
    }
}
