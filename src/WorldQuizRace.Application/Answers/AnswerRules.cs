using System.Text.Json;

namespace WorldQuizRace.Application.Answers;

/// <summary>Comparison rules shared by several validators (choice, number, text, mapping, set).</summary>
public static class AnswerRules
{
    public static bool ChoiceMatches(JsonElement correct, JsonElement submitted) =>
        AnswerReader.String(correct, "optionId") is { } expected
        && string.Equals(expected, AnswerReader.String(submitted, "optionId"), StringComparison.Ordinal);

    public static bool NumberMatches(JsonElement correct, JsonElement submitted)
    {
        var expected = AnswerReader.Number(correct, "value");
        var tolerance = AnswerReader.Number(correct, "tolerance") ?? 0m;
        var actual = AnswerReader.Number(submitted, "value");
        return expected is not null && actual is not null && Math.Abs(actual.Value - expected.Value) <= tolerance;
    }

    public static bool TextMatches(JsonElement correct, JsonElement submitted)
    {
        var accepted = AnswerReader.StringList(correct, "acceptedAnswers");
        var text = AnswerReader.NormalizeText(AnswerReader.String(submitted, "text"));
        return accepted is { Count: > 0 } && text.Length > 0 && accepted.Any(a => AnswerReader.NormalizeText(a) == text);
    }

    /// <summary>Same keys and same value for every key (item → target).</summary>
    public static bool MappingMatches(JsonElement correct, JsonElement submitted, string property)
    {
        var expected = AnswerReader.StringMap(correct, property);
        var actual = AnswerReader.StringMap(submitted, property);
        return expected is { Count: > 0 } && actual is not null && expected.Count == actual.Count
            && expected.All(pair => actual.TryGetValue(pair.Key, out var value) && value == pair.Value);
    }

    /// <summary>Same elements, in any order, without duplicates.</summary>
    public static bool SetMatches(IReadOnlyCollection<string>? expected, IReadOnlyCollection<string>? actual) =>
        expected is { Count: > 0 } && actual is not null
        && actual.Count == actual.Distinct(StringComparer.Ordinal).Count()
        && expected.ToHashSet(StringComparer.Ordinal).SetEquals(actual);
}
