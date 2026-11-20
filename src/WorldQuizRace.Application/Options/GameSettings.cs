using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Options;

/// <summary>Paramètres de jeu liés à la section "GameSettings" d'appsettings.json, validés au démarrage.</summary>
public sealed class GameSettings
{
    public const string SectionName = "GameSettings";

    /// <summary>Nombre de checkpoints par route (le dernier est la destination commune).</summary>
    public int CheckpointsPerRoute { get; set; }

    public TurnMode TurnMode { get; set; } = TurnMode.Alternating;

    public PlayerSlot FirstPlayer { get; set; } = PlayerSlot.A;

    /// <summary>Règles de difficulté par position ; elles doivent couvrir 1..CheckpointsPerRoute sans trou ni chevauchement.</summary>
    public List<DifficultyRule> DifficultyRules { get; set; } = [];

    /// <summary>Temps limite par défaut, en secondes, pour chaque type de défi.</summary>
    public Dictionary<ChallengeType, int> DefaultTimeLimitsSeconds { get; set; } = [];
}
