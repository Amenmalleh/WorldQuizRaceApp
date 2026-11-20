using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Options;

/// <summary>Game settings bound to the "GameSettings" section of appsettings.json and validated at startup.</summary>
public sealed class GameSettings
{
    public const string SectionName = "GameSettings";

    /// <summary>Number of checkpoints per route (the last one is the shared final destination).</summary>
    public int CheckpointsPerRoute { get; set; }

    public TurnMode TurnMode { get; set; } = TurnMode.Alternating;

    public PlayerSlot FirstPlayer { get; set; } = PlayerSlot.A;

    /// <summary>Difficulty rules per position; they must cover 1..CheckpointsPerRoute with no gap and no overlap.</summary>
    public List<DifficultyRule> DifficultyRules { get; set; } = [];

    /// <summary>Default time limit, in seconds, for each challenge type.</summary>
    public Dictionary<ChallengeType, int> DefaultTimeLimitsSeconds { get; set; } = [];
}
