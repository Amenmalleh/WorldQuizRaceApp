using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Progression;

public interface IWinnerService
{
    /// <summary>
    /// The first player to reach the final checkpoint wins: the match becomes Finished with EndDate and WinnerId.
    /// Returns true when this call ended the match.
    /// </summary>
    bool TryDeclareWinner(Match match, MatchPlayer participant, int totalCheckpoints, DateTimeOffset now);
}

public sealed class WinnerService : IWinnerService
{
    public bool TryDeclareWinner(Match match, MatchPlayer participant, int totalCheckpoints, DateTimeOffset now)
    {
        if (match.Status != MatchStatus.InProgress || participant.CurrentPosition < totalCheckpoints)
        {
            return false;
        }

        match.Status = MatchStatus.Finished;
        match.EndDate = now;
        match.WinnerId = participant.PlayerId;
        match.CurrentTurnPlayerId = null;
        return true;
    }
}
