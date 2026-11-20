using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Match between two players. Players, their routes and positions are stored on
/// <see cref="MatchPlayer"/> (one per slot A/B): nothing is duplicated here.
/// </summary>
public class Match
{
    public int Id { get; set; }

    public TurnMode TurnMode { get; set; } = TurnMode.Alternating;

    public MatchStatus Status { get; set; } = MatchStatus.Created;

    /// <summary>Player whose turn it is (null before the start or after the end).</summary>
    public int? CurrentTurnPlayerId { get; set; }

    public Player? CurrentTurnPlayer { get; set; }

    /// <summary>Current turn number (0 before the first turn).</summary>
    public int TurnNumber { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public int? WinnerId { get; set; }

    public Player? Winner { get; set; }

    /// <summary>Jeton de concurrence optimiste (rowversion SQL Server).</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<MatchPlayer> Players { get; set; } = [];
}
