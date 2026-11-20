using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Application.Services.Progression;

public sealed record ProgressionResult(int PreviousPosition, int NewPosition)
{
    public bool Advanced => NewPosition > PreviousPosition;
}

public interface IProgressionService
{
    /// <summary>
    /// Records an answer on the participant: a correct answer moves exactly one checkpoint forward,
    /// a wrong (or late) answer never moves the player, and nobody goes past the final checkpoint.
    /// </summary>
    ProgressionResult Apply(MatchPlayer participant, bool isCorrect, int totalCheckpoints);
}

public sealed class ProgressionService : IProgressionService
{
    public ProgressionResult Apply(MatchPlayer participant, bool isCorrect, int totalCheckpoints)
    {
        if (participant.CurrentPosition >= totalCheckpoints)
        {
            throw new InvalidOperationException("The player already reached the final checkpoint.");
        }

        var previous = participant.CurrentPosition;
        participant.Attempts++;
        if (isCorrect)
        {
            participant.CorrectAnswers++;
            participant.CurrentPosition = Math.Min(previous + 1, totalCheckpoints);
        }
        else
        {
            participant.WrongAnswers++;
        }

        return new ProgressionResult(previous, participant.CurrentPosition);
    }
}
