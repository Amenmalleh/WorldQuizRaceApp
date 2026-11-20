using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"text":"La Loire"}; compared case-, accent- and whitespace-insensitively.</summary>
public sealed class TextAnswerValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.TextAnswer;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerRules.TextMatches(correctAnswer, submitted);
}
