using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"value":true}. The strings "true"/"false" are accepted too.</summary>
public sealed class TrueFalseValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.TrueFalse;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted)
    {
        var expected = ReadBoolean(AnswerReader.Property(correctAnswer, "value"));
        var actual = ReadBoolean(AnswerReader.Property(submitted, "value"));
        return expected is not null && actual == expected;
    }

    private static bool? ReadBoolean(JsonElement? value) => value?.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String when bool.TryParse(value.Value.GetString()?.Trim(), out var parsed) => parsed,
        _ => null
    };
}
