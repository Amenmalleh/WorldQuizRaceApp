using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Défi présenté à un joueur pendant une partie. Une ligne <see cref="AttemptStatus.Pending"/>
/// représente le défi en cours : au plus une par (MatchId, PlayerId), ce qui bloque les doubles soumissions.
/// </summary>
public class PlayerChallenge
{
    public const int AnswerMaxLength = 2000;

    public int Id { get; set; }

    public int MatchId { get; set; }

    public int PlayerId { get; set; }

    /// <summary>Participation (MatchId, PlayerId) : garantit que le joueur fait bien partie de la partie.</summary>
    public MatchPlayer MatchPlayer { get; set; } = null!;

    public int ChallengeId { get; set; }

    public Challenge Challenge { get; set; } = null!;

    /// <summary>Checkpoint visé par ce défi (position actuelle + 1).</summary>
    public int CheckpointPosition { get; set; }

    public int TurnNumber { get; set; }

    public AttemptStatus Status { get; set; } = AttemptStatus.Pending;

    /// <summary>null tant que le défi est en attente.</summary>
    public bool? IsCorrect { get; set; }

    /// <summary>Réponse soumise (JSON), null tant que le défi est en attente.</summary>
    public string? Answer { get; set; }

    public DateTimeOffset ServedAt { get; set; }

    public DateTimeOffset? AnsweredAt { get; set; }
}
