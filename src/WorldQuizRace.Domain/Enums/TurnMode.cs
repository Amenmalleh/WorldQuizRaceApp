namespace WorldQuizRace.Domain.Enums;

/// <summary>
/// Mode d'enchaînement des tours. Seul le mode alterné existe en V1 ;
/// d'autres modes (simultané…) pourront être ajoutés sans casser la configuration.
/// </summary>
public enum TurnMode
{
    Alternating = 1
}
