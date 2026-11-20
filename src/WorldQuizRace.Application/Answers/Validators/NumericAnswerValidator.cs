using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"value":12} or {"value":"3,5"}; correct when |value - expected| ≤ tolerance.</summary>
public sealed class NumericAnswerValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.NumericAnswer;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerRules.NumberMatches(correctAnswer, submitted);
}
