namespace WorldQuizRace.Domain.Enums;

/// <summary>
/// Supported challenge types (specification §6).
/// Adding a value here (plus its time limit in GameSettings and a validator) extends the catalog.
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
    VisualChallenge = 14
}
