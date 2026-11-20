using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission depends on content.answerType: choice {"optionId"}, number {"value"} or text {"text"}.</summary>
public sealed class LogicPuzzleValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.LogicPuzzle;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerReader.String(content, "answerType") switch
        {
            "choice" => AnswerRules.ChoiceMatches(correctAnswer, submitted),
            "number" => AnswerRules.NumberMatches(correctAnswer, submitted),
            "text" => AnswerRules.TextMatches(correctAnswer, submitted),
            _ => false
        };
}
