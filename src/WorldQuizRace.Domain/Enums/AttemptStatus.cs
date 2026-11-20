namespace WorldQuizRace.Domain.Enums;

/// <summary>
/// État d'une tentative de défi. Pending = défi actuellement présenté au joueur
/// (au plus un par joueur et par partie, garanti par un index unique filtré).
/// </summary>
public enum AttemptStatus
{
    Pending = 1,
    Answered = 2,
    TimedOut = 3
}
