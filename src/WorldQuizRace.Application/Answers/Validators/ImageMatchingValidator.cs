using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"pairs":{"l1":"r2",…}}; the set of left→right pairs must be identical.</summary>
public sealed class ImageMatchingValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.ImageMatching;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerRules.MappingMatches(correctAnswer, submitted, "pairs");
}
