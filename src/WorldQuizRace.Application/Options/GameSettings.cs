using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Options;

/// <summary>Game settings bound to the "GameSettings" section of appsettings.json and validated at startup.</summary>
public sealed class GameSettings
{
    public const string SectionName = "GameSettings";

    /// <summary>Number of checkpoints per route (the last one is the shared final destination).</summary>
    public int CheckpointsPerRoute { get; set; }

    public TurnMode TurnMode { get; set; } = TurnMode.Alternating;

    /// <summary>Who plays first: A, B or Random.</summary>
    public FirstPlayerOption FirstPlayer { get; set; } = FirstPlayerOption.A;

    /// <summary>Route code assigned to player A (starts in the far west).</summary>
    public string RouteForPlayerA { get; set; } = "WEST";

    /// <summary>Route code assigned to player B (starts in the far east).</summary>
    public string RouteForPlayerB { get; set; } = "EAST";

    /// <summary>Network/latency tolerance added to every time limit before an answer is considered late.</summary>
    public int AnswerGracePeriodSeconds { get; set; } = 2;

    /// <summary>Avatar keys a player may choose (the avatar itself is optional).</summary>
    public List<string> AllowedAvatars { get; set; } = [];

    /// <summary>Difficulty rules per position; they must cover 1..CheckpointsPerRoute with no gap and no overlap.</summary>
    public List<DifficultyRule> DifficultyRules { get; set; } = [];

    /// <summary>Default time limit, in seconds, for each challenge type.</summary>
    public Dictionary<ChallengeType, int> DefaultTimeLimitsSeconds { get; set; } = [];

    public RewardSettings Rewards { get; set; } = new();
}
