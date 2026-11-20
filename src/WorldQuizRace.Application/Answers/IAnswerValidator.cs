using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers;

/// <summary>
/// Strategy validating the answers of one <see cref="ChallengeType"/>, following docs/CHALLENGE_FORMATS.md.
/// Implementations are discovered automatically: adding a challenge type only requires adding a class.
/// </summary>
public interface IAnswerValidator
{
    ChallengeType Type { get; }

    /// <param name="content">Public content of the challenge.</param>
    /// <param name="correctAnswer">Secret expected answer.</param>
    /// <param name="submitted">Answer sent by the player (already known to be a JSON object).</param>
    bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted);
}

public interface IAnswerValidationService
{
    /// <summary>Returns true only for a well-formed, correct answer. Never throws for a malformed answer.</summary>
    bool Validate(ChallengeType type, string contentJson, string correctAnswerJson, JsonElement submitted);
}
