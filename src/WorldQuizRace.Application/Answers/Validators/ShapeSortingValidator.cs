using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"mapping":{"s1":"b1",…}}; every shape must be in its expected bin.</summary>
public sealed class ShapeSortingValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.ShapeSorting;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerRules.MappingMatches(correctAnswer, submitted, "mapping");
}
