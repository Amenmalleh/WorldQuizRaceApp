using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Educational challenge. The JSON format of <see cref="Content"/> and <see cref="CorrectAnswer"/>
/// depends on <see cref="Type"/>: see docs/CHALLENGE_FORMATS.md.
/// </summary>
public class Challenge
{
    public const int CodeMaxLength = 20;
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 300;
    public const int ExplanationMaxLength = 300;

    public int Id { get; set; }

    /// <summary>Stable identifier in the seed file (e.g. "G2-M-07"), used for idempotent synchronization.</summary>
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ChallengeType Type { get; set; }

    public int AgeGroupId { get; set; }

    public AgeGroup AgeGroup { get; set; } = null!;

    public Difficulty Difficulty { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    /// <summary>PUBLIC JSON sent to the browser: prompt, options, items… Never contains the answer.</summary>
    public string Content { get; set; } = "{}";

    /// <summary>SECRET JSON: never leaves the server, used only for server-side validation.</summary>
    public string CorrectAnswer { get; set; } = "{}";

    /// <summary>Short educational sentence shown after the answer.</summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>Specific time limit; null = GameSettings default for this type.</summary>
    public int? TimeLimitSeconds { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PlayerChallenge> Attempts { get; set; } = [];
}
