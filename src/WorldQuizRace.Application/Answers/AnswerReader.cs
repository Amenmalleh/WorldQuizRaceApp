using System.Globalization;
using System.Text;
using System.Text.Json;

namespace WorldQuizRace.Application.Answers;

/// <summary>
/// Lenient helpers used by the answer validators. Every method returns null/false instead of throwing
/// when the JSON does not have the expected shape: a malformed answer is simply a wrong answer.
/// </summary>
public static class AnswerReader
{
    private static readonly char[] TrimmedPunctuation = ['.', ',', '!', '?', ';', ':', '"', '«', '»', ' '];

    public static JsonElement? Property(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    public static string? String(JsonElement element, string name) =>
        Property(element, name) is { } value ? AsText(value) : null;

    /// <summary>Reads a string list (array of strings or numbers); null when absent or of another shape.</summary>
    public static List<string>? StringList(JsonElement element, string name)
    {
        if (Property(element, name) is not { ValueKind: JsonValueKind.Array } array)
        {
            return null;
        }

        var result = new List<string>();
        foreach (var item in array.EnumerateArray())
        {
            if (AsText(item) is not { } text)
            {
                return null;
            }

            result.Add(text);
        }

        return result;
    }

    /// <summary>Reads an object whose values are strings (e.g. {"i1":"t2"}); null when absent or of another shape.</summary>
    public static Dictionary<string, string>? StringMap(JsonElement element, string name)
    {
        if (Property(element, name) is not { ValueKind: JsonValueKind.Object } map)
        {
            return null;
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in map.EnumerateObject())
        {
            if (AsText(property.Value) is not { } text || !result.TryAdd(property.Name, text))
            {
                return null;
            }
        }

        return result;
    }

    public static decimal? Number(JsonElement element, string name) =>
        Property(element, name) is { } value ? AsNumber(value) : null;

    /// <summary>Accepts JSON numbers and numeric strings: "3,5" = "3.5", spaces as thousands separators are ignored.</summary>
    public static decimal? AsNumber(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
        JsonValueKind.String => ParseNumber(value.GetString()),
        _ => null
    };

    public static decimal? ParseNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var compact = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (compact.Count(c => c is ',' or '.') > 1)
        {
            return null;
        }

        compact = compact.Replace(',', '.').Replace('−', '-');
        return decimal.TryParse(compact, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    /// <summary>
    /// Text normalization shared by every text comparison: Unicode NFKD, accents removed, lower case,
    /// typographic apostrophes unified, whitespace collapsed, surrounding punctuation ignored.
    /// </summary>
    public static string NormalizeText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var decomposed = text.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(c is '’' or '‘' or '`' ? '\'' : c);
        }

        var collapsed = string.Join(' ', builder.ToString().ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Trim(TrimmedPunctuation);
    }

    /// <summary>Compares two scalar answers: numerically when both are numbers, otherwise as normalized text.</summary>
    public static bool ScalarEquals(string expected, string submitted)
    {
        if (ParseNumber(expected) is { } a && ParseNumber(submitted) is { } b)
        {
            return a == b;
        }

        return NormalizeText(expected) == NormalizeText(submitted);
    }

    private static string? AsText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        _ => null
    };
}
