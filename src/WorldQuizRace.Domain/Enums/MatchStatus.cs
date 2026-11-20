namespace WorldQuizRace.Domain.Enums;

/// <summary>Cycle de vie d'une partie.</summary>
public enum MatchStatus
{
    Created = 1,
    InProgress = 2,
    Finished = 3,
    Abandoned = 4
}
