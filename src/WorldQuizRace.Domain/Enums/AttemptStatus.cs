namespace WorldQuizRace.Domain.Enums;

/// <summary>
/// State of a challenge attempt. Pending = challenge currently shown to the player
/// (at most one per player and match, enforced by a filtered unique index).
/// </summary>
public enum AttemptStatus
{
    Pending = 1,
    Answered = 2,
    TimedOut = 3
}
