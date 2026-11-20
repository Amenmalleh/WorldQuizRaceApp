using Microsoft.Extensions.Options;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Options;

/// <summary>
/// Validates the consistency of <see cref="GameSettings"/>. Runs at startup (ValidateOnStart):
/// an invalid configuration stops the application instead of producing an inconsistent match.
/// </summary>
public sealed class GameSettingsValidator : IValidateOptions<GameSettings>
{
    public const int MinCheckpoints = 2;
    public const int MaxCheckpoints = 50;
    public const int MinTimeLimitSeconds = 5;
    public const int MaxTimeLimitSeconds = 600;

    public ValidateOptionsResult Validate(string? name, GameSettings options)
    {
        var errors = new List<string>();

        if (options.CheckpointsPerRoute is < MinCheckpoints or > MaxCheckpoints)
        {
            errors.Add($"CheckpointsPerRoute must be between {MinCheckpoints} and {MaxCheckpoints} (actual: {options.CheckpointsPerRoute}).");
        }

        if (!Enum.IsDefined(options.TurnMode))
        {
            errors.Add($"TurnMode '{options.TurnMode}' is not supported.");
        }

        if (!Enum.IsDefined(options.FirstPlayer))
        {
            errors.Add($"FirstPlayer '{options.FirstPlayer}' must be A, B or Random.");
        }

        if (string.IsNullOrWhiteSpace(options.RouteForPlayerA) || string.IsNullOrWhiteSpace(options.RouteForPlayerB)
            || string.Equals(options.RouteForPlayerA, options.RouteForPlayerB, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("RouteForPlayerA and RouteForPlayerB must be two different route codes.");
        }

        if (options.AnswerGracePeriodSeconds is < 0 or > 30)
        {
            errors.Add($"AnswerGracePeriodSeconds must be between 0 and 30 (actual: {options.AnswerGracePeriodSeconds}).");
        }

        if (options.AllowedAvatars.Count == 0 || options.AllowedAvatars.Any(a => string.IsNullOrWhiteSpace(a) || a.Length > 32))
        {
            errors.Add("AllowedAvatars must contain at least one non-empty key of at most 32 characters.");
        }

        ValidateDifficultyRules(options, errors);
        ValidateTimeLimits(options, errors);
        ValidateRewards(options.Rewards, errors);

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateDifficultyRules(GameSettings options, List<string> errors)
    {
        if (options.DifficultyRules.Count == 0)
        {
            errors.Add("DifficultyRules must contain at least one rule.");
            return;
        }

        foreach (var rule in options.DifficultyRules)
        {
            if (!Enum.IsDefined(rule.Difficulty))
            {
                errors.Add($"DifficultyRules: unknown difficulty '{rule.Difficulty}'.");
            }

            if (rule.FromPosition > rule.ToPosition)
            {
                errors.Add($"DifficultyRules: FromPosition ({rule.FromPosition}) must be <= ToPosition ({rule.ToPosition}).");
            }

            if (rule.FromPosition < 1 || rule.ToPosition > options.CheckpointsPerRoute)
            {
                errors.Add($"DifficultyRules: range {rule.FromPosition}-{rule.ToPosition} must stay within 1..{options.CheckpointsPerRoute}.");
            }
        }

        // Every position 1..N must be covered by exactly one rule (no gap, no overlap).
        for (var position = 1; position <= options.CheckpointsPerRoute; position++)
        {
            var matches = options.DifficultyRules.Count(r => position >= r.FromPosition && position <= r.ToPosition);
            if (matches != 1)
            {
                errors.Add($"DifficultyRules: checkpoint position {position} is covered by {matches} rule(s); exactly 1 expected.");
            }
        }
    }

    private static void ValidateRewards(RewardSettings rewards, List<string> errors)
    {
        int[] amounts =
        [
            rewards.PointsPerCorrectAnswer, rewards.SpeedBonusPoints, rewards.StarsPerCorrectAnswer,
            rewards.SpeedBonusStars, rewards.CoinsPerCorrectAnswer, rewards.CoinsForReachingDestination
        ];

        if (amounts.Any(a => a < 0))
        {
            errors.Add("Rewards: amounts cannot be negative.");
        }

        if (rewards.SpeedBonusMaxTimeRatio is <= 0 or > 1)
        {
            errors.Add("Rewards: SpeedBonusMaxTimeRatio must be in ]0, 1].");
        }
    }

    private static void ValidateTimeLimits(GameSettings options, List<string> errors)
    {
        foreach (var type in Enum.GetValues<ChallengeType>())
        {
            if (!options.DefaultTimeLimitsSeconds.TryGetValue(type, out var seconds))
            {
                errors.Add($"DefaultTimeLimitsSeconds: missing time limit for challenge type '{type}'.");
            }
            else if (seconds is < MinTimeLimitSeconds or > MaxTimeLimitSeconds)
            {
                errors.Add($"DefaultTimeLimitsSeconds: '{type}' must be between {MinTimeLimitSeconds} and {MaxTimeLimitSeconds} seconds (actual: {seconds}).");
            }
        }
    }
}
