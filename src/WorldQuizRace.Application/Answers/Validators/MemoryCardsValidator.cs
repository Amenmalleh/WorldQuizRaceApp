using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>Submission: {"pairs":[["c1","c3"],…]}; same set of pairs, order ignored inside and between pairs.</summary>
public sealed class MemoryCardsValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.MemoryCards;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted) =>
        AnswerRules.SetMatches(ReadPairs(correctAnswer), ReadPairs(submitted));

    /// <summary>Each pair becomes an order-independent key such as "c1|c3"; null when the shape is wrong.</summary>
    private static List<string>? ReadPairs(JsonElement element)
    {
        if (AnswerReader.Property(element, "pairs") is not { ValueKind: JsonValueKind.Array } pairs)
        {
            return null;
        }

        var keys = new List<string>();
        foreach (var pair in pairs.EnumerateArray())
        {
            if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2)
            {
                return null;
            }

            var ids = pair.EnumerateArray().Select(id => id.ValueKind == JsonValueKind.String ? id.GetString() : null).ToList();
            if (ids.Any(string.IsNullOrEmpty) || ids[0] == ids[1])
            {
                return null;
            }

            keys.Add(string.Join('|', ids.Order(StringComparer.Ordinal)));
        }

        return keys;
    }
}
