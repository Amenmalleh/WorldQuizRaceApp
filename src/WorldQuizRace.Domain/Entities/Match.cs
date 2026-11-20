using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Partie entre deux joueurs. Les joueurs, leurs routes et leurs positions sont portés par
/// <see cref="MatchPlayer"/> (un par emplacement A/B) : rien n'est dupliqué ici.
/// </summary>
public class Match
{
    public int Id { get; set; }

    public TurnMode TurnMode { get; set; } = TurnMode.Alternating;

    public MatchStatus Status { get; set; } = MatchStatus.Created;

    /// <summary>Joueur dont c'est le tour (null avant le début ou après la fin).</summary>
    public int? CurrentTurnPlayerId { get; set; }

    public Player? CurrentTurnPlayer { get; set; }

    /// <summary>Numéro du tour en cours (0 avant le premier tour).</summary>
    public int TurnNumber { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public int? WinnerId { get; set; }

    public Player? Winner { get; set; }

    /// <summary>Jeton de concurrence optimiste (rowversion SQL Server).</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<MatchPlayer> Players { get; set; } = [];
}
