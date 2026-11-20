using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Options;

/// <summary>Associe une plage de positions de checkpoint (bornes incluses) à un niveau de difficulté.</summary>
public sealed class DifficultyRule
{
    public int FromPosition { get; set; }

    public int ToPosition { get; set; }

    public Difficulty Difficulty { get; set; }
}
