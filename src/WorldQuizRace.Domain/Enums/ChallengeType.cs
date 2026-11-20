namespace WorldQuizRace.Domain.Enums;

/// <summary>
/// Types de défis supportés (cahier des charges §6).
/// Ajouter une valeur ici (et son temps limite dans GameSettings) suffit pour étendre le catalogue.
/// </summary>
public enum ChallengeType
{
    MultipleChoice = 1,
    TrueFalse = 2,
    TextAnswer = 3,
    NumericAnswer = 4,
    DragAndDrop = 5,
    ShapeSorting = 6,
    ImageMatching = 7,
    MemoryCards = 8,
    SequenceCompletion = 9,
    LogicPuzzle = 10,
    Maze = 11,
    OrderingClassification = 12,
    MathMiniGame = 13,
    InteractiveVisual = 14
}
