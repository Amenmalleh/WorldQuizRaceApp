using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Dtos;

// ---------- Requests ----------

/// <summary>Registration data: a nickname and an age only (no personal data), plus an optional avatar key.</summary>
public sealed record RegisterPlayerRequest(string? Name, int Age, string? Avatar);

/// <summary>Registers both players and creates their match in one call.</summary>
public sealed record CreateMatchRequest(RegisterPlayerRequest? PlayerA, RegisterPlayerRequest? PlayerB);

/// <summary>Answer to the pending challenge. <see cref="Answer"/> is the raw JSON described in docs/CHALLENGE_FORMATS.md.</summary>
public sealed record SubmitAnswerRequest(int MatchId, int PlayerChallengeId, JsonElement Answer);

// ---------- Players ----------

public sealed record PlayerDto(int Id, string Name, int Age, int AgeGroupId, string AgeGroupName, string? Avatar);

public sealed record AgeGroupDto(int Id, string Name, int MinimumAge, int MaximumAge);

// ---------- Match state ----------

public sealed record CheckpointDto(int Position, string Name, string Country, string FunFact, double MapX, double MapY, bool IsFinal);

public sealed record RouteDto(int Id, string Code, string Name, string? Description, int TotalCheckpoints, IReadOnlyList<CheckpointDto> Checkpoints);

public sealed record MatchPlayerStateDto(
    PlayerDto Player,
    PlayerSlot Slot,
    RouteDto Route,
    int Position,
    int CorrectAnswers,
    int WrongAnswers,
    int Attempts,
    int Score,
    int Stars,
    int Coins,
    bool IsCurrentTurn,
    bool HasPendingChallenge);

public sealed record MatchStateDto(
    int MatchId,
    MatchStatus Status,
    TurnMode TurnMode,
    int TurnNumber,
    int? CurrentTurnPlayerId,
    PlayerSlot? CurrentTurnSlot,
    int TotalCheckpoints,
    int? WinnerId,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    IReadOnlyList<MatchPlayerStateDto> Players);

// ---------- Challenges ----------

/// <summary>
/// What the browser receives for a challenge. It deliberately has no CorrectAnswer and no Explanation:
/// the explanation is only revealed in <see cref="AnswerResultDto"/>, after the answer.
/// </summary>
public sealed record ChallengeDto(
    int PlayerChallengeId,
    int MatchId,
    int PlayerId,
    ChallengeType Type,
    Difficulty Difficulty,
    string Category,
    string Title,
    string Prompt,
    JsonElement Content,
    int CheckpointPosition,
    int TimeLimitSeconds,
    DateTimeOffset ServedAt,
    DateTimeOffset ExpiresAt,
    int RemainingSeconds);

// ---------- Answers and rewards ----------

public sealed record BadgeDto(string Code, string Name, string Description, string Icon);

public sealed record RewardDto(int Points, int Stars, int Coins, IReadOnlyList<BadgeDto> Badges)
{
    public static RewardDto None { get; } = new(0, 0, 0, []);
}

public sealed record AnswerResultDto(
    int PlayerChallengeId,
    bool IsCorrect,
    bool TimedOut,
    string Explanation,
    int PreviousPosition,
    int NewPosition,
    bool IsVictory,
    RewardDto Reward,
    MatchStateDto State);

// ---------- Results ----------

public sealed record PlayerResultDto(
    PlayerDto Player,
    PlayerSlot Slot,
    bool IsWinner,
    int FinalPosition,
    int TotalCheckpoints,
    int CorrectAnswers,
    int WrongAnswers,
    int ChallengesAttempted,
    int ChallengesCompleted,
    double AccuracyPercent,
    TimeSpan TimeSpentAnswering,
    int Score,
    int Stars,
    int Coins,
    IReadOnlyList<BadgeDto> Badges,
    string Message);

public sealed record MatchResultDto(
    int MatchId,
    MatchStatus Status,
    int? WinnerId,
    string DestinationName,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    TimeSpan TotalDuration,
    IReadOnlyList<PlayerResultDto> Players);
