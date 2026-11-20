using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"selectedIds":["v1","v3"]}; same set of cells, in any order.</summary>
public sealed class VisualChallengeValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.VisualChallenge;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerRules.SetMatches(
            AnswerReader.StringList(correctAnswer, "selectedIds"),
            AnswerReader.StringList(submitted, "selectedIds"));
}
