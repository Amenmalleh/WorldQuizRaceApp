using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"mapping":{"i1":"t2",…}}; every item must be on its expected target.</summary>
public sealed class DragAndDropValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.DragAndDrop;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerRules.MappingMatches(correctAnswer, submitted, "mapping");
}
