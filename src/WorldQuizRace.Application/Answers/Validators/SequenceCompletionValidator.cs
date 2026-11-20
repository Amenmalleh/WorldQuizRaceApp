using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"values":["11","17"]}; one value per blank, in order (numbers compared numerically).</summary>
public sealed class SequenceCompletionValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.SequenceCompletion;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted)
    {
        var expected = AnswerReader.StringList(correctAnswer, "values");
        var actual = AnswerReader.StringList(submitted, "values");
        return expected is { Count: > 0 } && actual is not null && expected.Count == actual.Count
            && expected.Zip(actual).All(pair => AnswerReader.ScalarEquals(pair.First, pair.Second));
    }
}
