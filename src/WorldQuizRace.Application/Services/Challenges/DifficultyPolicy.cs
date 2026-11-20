using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Challenges;

public interface IDifficultyPolicy
{
    /// <summary>
    /// Difficulty of the challenge guarding a checkpoint position. The same rule applies to both players:
    /// at equal positions they get the same RELATIVE difficulty, each within their own age group.
    /// </summary>
    Difficulty ForPosition(int position);

    /// <summary>Fallback order when a difficulty is exhausted: the target, then the closest ones (easier first on a tie).</summary>
    IReadOnlyList<Difficulty> FallbackOrder(Difficulty target);
}

public sealed class DifficultyPolicy(IOptions<GameSettings> options) : IDifficultyPolicy
{
    public Difficulty ForPosition(int position) =>
        options.Value.DifficultyRules.FirstOrDefault(r => position >= r.FromPosition && position <= r.ToPosition)?.Difficulty
        ?? throw new ArgumentOutOfRangeException(nameof(position), position, "No difficulty rule covers this position.");

    public IReadOnlyList<Difficulty> FallbackOrder(Difficulty target) =>
        Enum.GetValues<Difficulty>()
            .OrderBy(d => Math.Abs((int)d - (int)target))
            .ThenBy(d => (int)d)
            .ToList();
}
