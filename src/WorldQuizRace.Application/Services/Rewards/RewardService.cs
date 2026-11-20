using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Rewards;

/// <summary>Data needed to reward one resolved attempt.</summary>
public sealed record RewardContext(
    Match Match,
    MatchPlayer Participant,
    PlayerChallenge Attempt,
    bool IsCorrect,
    int TimeLimitSeconds,
    int TotalCheckpoints);

public interface IRewardService
{
    /// <summary>
    /// Grants points, stars and coins for a correct answer (with a speed bonus) and awards newly earned badges.
    /// When the match just ended, end-of-match badges are also awarded to the other player.
    /// Changes are tracked on the context; the caller saves them. Returns the answering player's rewards.
    /// </summary>
    Task<RewardDto> ApplyAsync(RewardContext context, CancellationToken cancellationToken = default);
}

public sealed class RewardService(IApplicationDbContext db, IEnumerable<IBadgeRule> rules, IOptions<GameSettings> options)
    : IRewardService
{
    private const int HistoryLength = 10;

    public async Task<RewardDto> ApplyAsync(RewardContext context, CancellationToken cancellationToken = default)
    {
        var settings = options.Value.Rewards;
        var participant = context.Participant;
        int points = 0, stars = 0, coins = 0;

        if (context.IsCorrect)
        {
            points = settings.PointsPerCorrectAnswer;
            stars = settings.StarsPerCorrectAnswer;
            coins = settings.CoinsPerCorrectAnswer;

            var answeredIn = (context.Attempt.AnsweredAt ?? context.Attempt.ServedAt) - context.Attempt.ServedAt;
            if (answeredIn.TotalSeconds <= context.TimeLimitSeconds * settings.SpeedBonusMaxTimeRatio)
            {
                points += settings.SpeedBonusPoints;
                stars += settings.SpeedBonusStars;
            }

            if (participant.CurrentPosition >= context.TotalCheckpoints)
            {
                coins += settings.CoinsForReachingDestination;
            }
        }

        participant.Score += points;
        participant.Stars += stars;
        participant.Coins += coins;

        var matchFinished = context.Match.Status == MatchStatus.Finished;
        var badges = await AwardBadgesAsync(context.Match, participant, context.Attempt, context.TotalCheckpoints, matchFinished, cancellationToken);

        if (matchFinished)
        {
            foreach (var other in context.Match.Players.Where(p => p.PlayerId != participant.PlayerId))
            {
                await AwardBadgesAsync(context.Match, other, currentAttempt: null, context.TotalCheckpoints, matchFinished, cancellationToken);
            }
        }

        return new RewardDto(points, stars, coins, badges);
    }

    private async Task<IReadOnlyList<BadgeDto>> AwardBadgesAsync(
        Match match, MatchPlayer participant, PlayerChallenge? currentAttempt, int totalCheckpoints, bool matchFinished, CancellationToken ct)
    {
        // Database values: the current attempt is still Pending there, so its result is appended in memory.
        var history = await db.PlayerChallenges
            .AsNoTracking()
            .Where(pc => pc.MatchId == match.Id && pc.PlayerId == participant.PlayerId && pc.Status != AttemptStatus.Pending)
            .OrderByDescending(pc => pc.Id)
            .Take(HistoryLength)
            .Select(pc => pc.IsCorrect == true)
            .ToListAsync(ct);
        history.Reverse();
        if (currentAttempt?.IsCorrect is { } currentResult)
        {
            history.Add(currentResult);
        }

        var badgeContext = new BadgeContext(participant.CurrentPosition, totalCheckpoints, participant.CorrectAnswers, history, matchFinished);
        var earnedCodes = rules.Where(r => r.IsEarned(badgeContext)).Select(r => r.BadgeCode).ToList();
        if (earnedCodes.Count == 0)
        {
            return [];
        }

        var owned = await db.PlayerBadges
            .Where(pb => pb.PlayerId == participant.PlayerId)
            .Select(pb => pb.Badge.Code)
            .ToListAsync(ct);
        var newCodes = earnedCodes.Except(owned).ToList();
        if (newCodes.Count == 0)
        {
            return [];
        }

        // Badges missing from the database (not seeded) are ignored rather than failing the answer.
        var badges = await db.Badges.Where(b => newCodes.Contains(b.Code)).ToListAsync(ct);
        foreach (var badge in badges)
        {
            db.PlayerBadges.Add(new PlayerBadge { PlayerId = participant.PlayerId, BadgeId = badge.Id, AwardedAt = currentAttempt?.AnsweredAt ?? match.EndDate ?? match.StartDate });
        }

        return badges.Select(b => new BadgeDto(b.Code, b.Name, b.Description, b.Icon)).ToList();
    }
}
