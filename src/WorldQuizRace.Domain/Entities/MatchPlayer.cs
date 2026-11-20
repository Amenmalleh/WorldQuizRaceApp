using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Participation d'un joueur à une partie : emplacement, route suivie, position et statistiques.
/// Le temps écoulé n'est pas stocké : il se calcule à partir des dates de la partie et des tentatives.
/// </summary>
public class MatchPlayer
{
    /// <summary>Position de départ, avant le premier checkpoint.</summary>
    public const int StartPosition = 0;

    public int Id { get; set; }

    public int MatchId { get; set; }

    public Match Match { get; set; } = null!;

    public int PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public PlayerSlot Slot { get; set; }

    public int RouteId { get; set; }

    public Route Route { get; set; } = null!;

    /// <summary>0 = départ, N = destination finale atteinte.</summary>
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
