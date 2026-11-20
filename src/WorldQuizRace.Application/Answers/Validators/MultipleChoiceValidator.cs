using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"optionId":"b"}.</summary>
public sealed class MultipleChoiceValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.MultipleChoice;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerRules.ChoiceMatches(correctAnswer, submitted);
}
