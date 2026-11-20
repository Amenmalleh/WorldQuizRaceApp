using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers;

/// <summary>Dispatches each answer to the validator registered for its challenge type.</summary>
public sealed class AnswerValidationService : IAnswerValidationService
{
    private readonly IReadOnlyDictionary<ChallengeType, IAnswerValidator> _validators;
    private readonly ILogger<AnswerValidationService> _logger;

    public AnswerValidationService(IEnumerable<IAnswerValidator> validators, ILogger<AnswerValidationService> logger)
    {
        _logger = logger;
        var byType = new Dictionary<ChallengeType, IAnswerValidator>();
        foreach (var validator in validators)
        {
            if (!byType.TryAdd(validator.Type, validator))
            {
                throw new InvalidOperationException($"Two answer validators are registered for {validator.Type}.");
            }
        }

        _validators = byType;
    }

    public IReadOnlyCollection<ChallengeType> SupportedTypes => _validators.Keys.ToList();

    public bool Validate(ChallengeType type, string contentJson, string correctAnswerJson, JsonElement submitted)
    {
        if (!_validators.TryGetValue(type, out var validator))
        {
            // Configuration bug, not a player error: fail loudly.
            throw new InvalidOperationException($"No answer validator is registered for challenge type {type}.");
        }

        if (submitted.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        try
        {
            using var content = JsonDocument.Parse(contentJson);
            using var correct = JsonDocument.Parse(correctAnswerJson);
            return validator.Validate(content.RootElement, correct.RootElement, submitted);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException
                                       or KeyNotFoundException or ArgumentException or OverflowException or IndexOutOfRangeException)
        {
            // A malformed answer is a wrong answer, never a server error.
            _logger.LogDebug(ex, "Malformed answer for a {ChallengeType} challenge treated as wrong.", type);
            return false;
        }
    }
}
