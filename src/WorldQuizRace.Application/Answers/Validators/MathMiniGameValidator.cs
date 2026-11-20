using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"answers":{"p1":15,"p2":"7"}}; every problem must be answered correctly.</summary>
public sealed class MathMiniGameValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.MathMiniGame;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted)
    {
        var expected = AnswerReader.StringMap(correctAnswer, "answers");
        if (expected is not { Count: > 0 } || AnswerReader.Property(submitted, "answers") is not { ValueKind: JsonValueKind.Object } answers)
        {
            return false;
        }

        foreach (var (problemId, expectedValue) in expected)
        {
            var actual = AnswerReader.Property(answers, problemId) is { } value ? AnswerReader.AsNumber(value) : null;
            if (actual is null || actual != AnswerReader.ParseNumber(expectedValue))
            {
                return false;
            }
        }

        return true;
    }
}
