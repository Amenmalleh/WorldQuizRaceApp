namespace WorldQuizRace.Domain.Enums;

/// <summary>Lifecycle of a match.</summary>
public enum MatchStatus
{
    Created = 1,
    InProgress = 2,
    Finished = 3,
    Abandoned = 4
}
