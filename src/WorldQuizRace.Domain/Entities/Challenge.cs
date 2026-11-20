using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Défi pédagogique. Le format JSON de <see cref="Content"/> et <see cref="CorrectAnswer"/>
/// dépend de <see cref="Type"/> : voir docs/CHALLENGE_FORMATS.md.
/// </summary>
public class Challenge
{
    public const int CodeMaxLength = 20;
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 300;
    public const int ExplanationMaxLength = 300;

    public int Id { get; set; }

    /// <summary>Identifiant stable du défi dans le fichier de seed (ex. "G2-M-07"), utilisé pour la synchronisation idempotente.</summary>
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ChallengeType Type { get; set; }

    public int AgeGroupId { get; set; }

    public AgeGroup AgeGroup { get; set; } = null!;

    public Difficulty Difficulty { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    /// <summary>JSON PUBLIC envoyé au navigateur : énoncé, options, éléments… Ne contient jamais la réponse.</summary>
    public string Content { get; set; } = "{}";

    /// <summary>JSON SECRET : ne quitte jamais le serveur, sert uniquement à la validation côté serveur.</summary>
    public string CorrectAnswer { get; set; } = "{}";

    /// <summary>Courte phrase pédagogique affichée après la réponse.</summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>Temps limite spécifique ; null = valeur par défaut de GameSettings pour ce type.</summary>
    public int? TimeLimitSeconds { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PlayerChallenge> Attempts { get; set; } = [];
}
