using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Challenge served to a player during a match. A <see cref="AttemptStatus.Pending"/> row
/// is the current challenge: at most one per (MatchId, PlayerId), which blocks double submissions.
/// </summary>
public class PlayerChallenge
{
    public const int AnswerMaxLength = 2000;

    public int Id { get; set; }

    public int MatchId { get; set; }

    public int PlayerId { get; set; }

    /// <summary>Participation (MatchId, PlayerId): guarantees the player belongs to the match.</summary>
    public MatchPlayer MatchPlayer { get; set; } = null!;

    public int ChallengeId { get; set; }

    public Challenge Challenge { get; set; } = null!;

    /// <summary>Checkpoint targeted by this challenge (current position + 1).</summary>
    public int CheckpointPosition { get; set; }

    public int TurnNumber { get; set; }

    public AttemptStatus Status { get; set; } = AttemptStatus.Pending;

    /// <summary>null while the challenge is pending.</summary>
    public bool? IsCorrect { get; set; }

    /// <summary>Submitted answer (JSON), null while the challenge is pending.</summary>
    public string? Answer { get; set; }

    public DateTimeOffset ServedAt { get; set; }

    public DateTimeOffset? AnsweredAt { get; set; }
}
