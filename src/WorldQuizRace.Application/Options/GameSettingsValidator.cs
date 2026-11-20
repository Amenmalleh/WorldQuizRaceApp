using Microsoft.Extensions.Options;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Options;

/// <summary>
/// Valide la cohérence de <see cref="GameSettings"/>. Exécuté au démarrage (ValidateOnStart) :
/// une configuration invalide empêche l'application de démarrer plutôt que de produire une partie incohérente.
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
            errors.Add($"FirstPlayer '{options.FirstPlayer}' must be A or B.");
        }

        ValidateDifficultyRules(options, errors);
        ValidateTimeLimits(options, errors);

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

        // Chaque position 1..N doit être couverte par exactement une règle (ni trou, ni chevauchement).
        for (var position = 1; position <= options.CheckpointsPerRoute; position++)
        {
            var matches = options.DifficultyRules.Count(r => position >= r.FromPosition && position <= r.ToPosition);
            if (matches != 1)
            {
                errors.Add($"DifficultyRules: checkpoint position {position} is covered by {matches} rule(s); exactly 1 expected.");
            }
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
