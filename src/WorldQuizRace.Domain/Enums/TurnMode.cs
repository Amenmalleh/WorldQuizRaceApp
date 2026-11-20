namespace WorldQuizRace.Domain.Enums;

/// <summary>
/// How turns follow each other. Only the alternating mode exists in V1;
/// other modes (simultaneous…) can be added later without breaking the configuration.
/// </summary>
public enum TurnMode
{
    Alternating = 1
}
