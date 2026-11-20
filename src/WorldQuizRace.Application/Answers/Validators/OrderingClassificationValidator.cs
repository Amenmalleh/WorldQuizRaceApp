using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>
/// Mode "order": {"order":[…]} must be exactly the expected sequence.
/// Mode "classify": {"mapping":{…}} must put every item in its expected group.
/// </summary>
public sealed class OrderingClassificationValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.OrderingClassification;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted)
    {
        switch (AnswerReader.String(content, "mode"))
        {
            case "order":
                var expected = AnswerReader.StringList(correctAnswer, "order");
                var actual = AnswerReader.StringList(submitted, "order");
                return expected is { Count: > 0 } && actual is not null && expected.SequenceEqual(actual, StringComparer.Ordinal);
            case "classify":
                return AnswerRules.MappingMatches(correctAnswer, submitted, "mapping");
            default:
                return false;
        }
    }
}
