using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// A player's participation in a match: slot, route, position and statistics.
/// Elapsed time is not stored: it is computed from the match and attempt dates.
/// </summary>
public class MatchPlayer
{
    /// <summary>Starting position, before the first checkpoint.</summary>
    public const int StartPosition = 0;

    public int Id { get; set; }

    public int MatchId { get; set; }

    public Match Match { get; set; } = null!;

    public int PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public PlayerSlot Slot { get; set; }

    public int RouteId { get; set; }

    public Route Route { get; set; } = null!;

    /// <summary>0 = start, N = final destination reached.</summary>
    public int CurrentPosition { get; set; } = StartPosition;

    public int CorrectAnswers { get; set; }

    public int WrongAnswers { get; set; }

    public int Attempts { get; set; }

    public int Score { get; set; }

    public int Stars { get; set; }

    public int Coins { get; set; }

    /// <summary>Jeton de concurrence optimiste (rowversion SQL Server).</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<PlayerChallenge> ChallengeAttempts { get; set; } = [];
}
