using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Options;

/// <summary>Maps an inclusive range of checkpoint positions to a difficulty level.</summary>
public sealed class DifficultyRule
{
    public int FromPosition { get; set; }

    public int ToPosition { get; set; }

    public Difficulty Difficulty { get; set; }
}
