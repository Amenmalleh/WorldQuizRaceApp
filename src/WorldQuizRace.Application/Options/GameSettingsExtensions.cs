using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Application.Options;

public static class GameSettingsExtensions
{
    /// <summary>Time limit of a challenge: its own value, otherwise the default configured for its type.</summary>
    public static int TimeLimitFor(this GameSettings settings, Challenge challenge) =>
        challenge.TimeLimitSeconds
        ?? (settings.DefaultTimeLimitsSeconds.TryGetValue(challenge.Type, out var seconds)
            ? seconds
            : throw new InvalidOperationException($"No default time limit configured for {challenge.Type}."));
}
