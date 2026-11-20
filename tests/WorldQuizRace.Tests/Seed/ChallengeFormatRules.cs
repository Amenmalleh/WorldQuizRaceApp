using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Tests.Seed;

/// <summary>
/// Contrat de format de Content / CorrectAnswer par ChallengeType, tel que décrit dans docs/CHALLENGE_FORMATS.md.
/// Retourne la liste des violations (vide = défi valide), y compris des vérifications de justesse calculables
/// (labyrinthe résoluble, résultats des calculs).
/// </summary>
internal static class ChallengeFormatRules
{
    private static readonly HashSet<string> Shapes = ["circle", "square", "triangle", "star", "rectangle", "heart"];
    private static readonly HashSet<string> Colors = ["red", "blue", "green", "yellow", "orange", "purple"];

    public static List<string> Validate(ChallengeType type, JsonElement content, JsonElement answer)
    {
        var errors = new List<string>();
        RequireString(content, "prompt", errors);

        switch (type)
        {
            case ChallengeType.MultipleChoice:
                ValidateChoice(content, answer, errors);
                break;

            case ChallengeType.TrueFalse:
                if (Prop(answer, "value")?.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    errors.Add("answer.value must be a boolean");
                }

                break;

            case ChallengeType.TextAnswer:
                ValidateAcceptedAnswers(answer, errors);
                break;

            case ChallengeType.NumericAnswer:
                ValidateNumeric(answer, errors);
                break;

            case ChallengeType.DragAndDrop:
                ValidateMapping(content, answer, "items", "targets", errors);
                break;

            case ChallengeType.ShapeSorting:
                ValidateMapping(content, answer, "shapes", "bins", errors);
                foreach (var shape in Array(content, "shapes"))
                {
                    if (!Shapes.Contains(Str(shape, "shape") ?? "")) errors.Add($"unknown shape '{Str(shape, "shape")}'");
                    if (!Colors.Contains(Str(shape, "color") ?? "")) errors.Add($"unknown color '{Str(shape, "color")}'");
                }

                break;

            case ChallengeType.ImageMatching:
            {
                var left = Ids(content, "left", errors);
                var right = Ids(content, "right", errors);
                var pairs = Object(answer, "pairs");
                if (pairs is null) { errors.Add("answer.pairs must be an object"); break; }
                if (!left.SetEquals(pairs.Keys)) errors.Add("answer.pairs must map every left id");
                var targets = pairs.Values.ToList();
                if (targets.Distinct().Count() != targets.Count || !targets.All(right.Contains)) errors.Add("answer.pairs must be one-to-one onto right ids");
                break;
            }

            case ChallengeType.MemoryCards:
            {
                var cards = Ids(content, "cards", errors);
                if (cards.Count < 4 || cards.Count % 2 != 0) errors.Add("cards must contain an even number (>= 4) of cards");
                foreach (var card in Array(content, "cards")) RequireString(card, "face", errors);
                var pairs = Array(answer, "pairs").Select(p => p.EnumerateArray().Select(x => x.GetString()!).ToList()).ToList();
                var flat = pairs.SelectMany(p => p).ToList();
                if (pairs.Any(p => p.Count != 2) || flat.Count != cards.Count || !cards.SetEquals(flat)) errors.Add("answer.pairs must partition all cards in pairs");
                break;
            }

            case ChallengeType.SequenceCompletion:
            {
                var sequence = Array(content, "sequence").ToList();
                var blanks = sequence.Count(e => e.ValueKind == JsonValueKind.Null);
                var values = Array(answer, "values").Select(v => v.GetString()).ToList();
                if (blanks == 0) errors.Add("sequence must contain at least one null blank");
                if (values.Count != blanks) errors.Add("answer.values must have one value per blank");
                if (Prop(content, "options") is { } options && !values.All(v => options.EnumerateArray().Any(o => o.GetString() == v)))
                {
                    errors.Add("every answer value must be one of the options");
                }

                break;
            }

            case ChallengeType.LogicPuzzle:
                switch (Str(content, "answerType"))
                {
                    case "choice": ValidateChoice(content, answer, errors); break;
                    case "number": ValidateNumeric(answer, errors); break;
                    case "text": ValidateAcceptedAnswers(answer, errors); break;
                    default: errors.Add("answerType must be choice, number or text"); break;
                }

                break;

            case ChallengeType.Maze:
            {
                var grid = Array(content, "grid").Select(r => r.GetString()!).ToList();
                var declared = Prop(answer, "shortestPathLength")?.GetInt32();
                var actual = ShortestPath(grid, errors);
                if (actual is null) errors.Add("maze has no path from S to E");
                else if (declared != actual) errors.Add($"shortestPathLength is {declared} but BFS finds {actual}");
                break;
            }

            case ChallengeType.OrderingClassification:
            {
                var items = Ids(content, "items", errors);
                switch (Str(content, "mode"))
                {
                    case "order":
                        var order = Array(answer, "order").Select(x => x.GetString()!).ToList();
                        if (order.Count != items.Count || !items.SetEquals(order)) errors.Add("answer.order must list every item exactly once");
                        if (order.SequenceEqual(Array(content, "items").Select(i => Str(i, "id")!))) errors.Add("items are displayed in the correct order: the answer would be given away");
                        break;
                    case "classify":
                        ValidateMapping(content, answer, "items", "groups", errors);
                        break;
                    default:
                        errors.Add("mode must be order or classify");
                        break;
                }

                break;
            }

            case ChallengeType.MathMiniGame:
            {
                var expected = Object(answer, "answers");
                foreach (var problem in Array(content, "problems"))
                {
                    var id = Str(problem, "id")!;
                    var expression = Str(problem, "expression")!;
                    var computed = Evaluate(expression);
                    if (expected is null || !expected.TryGetValue(id, out var declaredText) || decimal.Parse(declaredText, CultureInfo.InvariantCulture) != computed)
                    {
                        errors.Add($"problem {id} '{expression}' = {computed}, declared {(expected?.GetValueOrDefault(id) ?? "nothing")}");
                    }
                }

                break;
            }

            case ChallengeType.VisualChallenge:
            {
                var cells = Ids(content, "cells", errors);
                var selection = Str(content, "selection");
                var selected = Array(answer, "selectedIds").Select(x => x.GetString()!).ToList();
                if (selection is not ("single" or "multiple")) errors.Add("selection must be single or multiple");
                if (selected.Count == 0 || !selected.All(cells.Contains)) errors.Add("answer.selectedIds must reference cells");
                if (selection == "single" && selected.Count != 1) errors.Add("single selection must have exactly one answer");
                break;
            }

            default:
                errors.Add($"no format rule for {type}");
                break;
        }

        return errors;
    }

    /// <summary>Normalisation des réponses texte : NFKD, sans accents, minuscules, espaces réduits.</summary>
    public static string NormalizeText(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
        }

        return string.Join(' ', builder.ToString().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static void ValidateChoice(JsonElement content, JsonElement answer, List<string> errors)
    {
        var options = Ids(content, "options", errors);
        if (options.Count < 2) errors.Add("at least two options are required");
        if (!options.Contains(Str(answer, "optionId") ?? "")) errors.Add("answer.optionId must reference an option");
    }

    private static void ValidateNumeric(JsonElement answer, List<string> errors)
    {
        if (Prop(answer, "value")?.ValueKind != JsonValueKind.Number) errors.Add("answer.value must be a number");
        if (Prop(answer, "tolerance") is { } t && (t.ValueKind != JsonValueKind.Number || t.GetDecimal() < 0)) errors.Add("answer.tolerance must be >= 0");
    }

    private static void ValidateAcceptedAnswers(JsonElement answer, List<string> errors)
    {
        var accepted = Array(answer, "acceptedAnswers").Select(a => a.GetString() ?? "").ToList();
        if (accepted.Count == 0) errors.Add("answer.acceptedAnswers must not be empty");
        foreach (var a in accepted.Where(a => a != NormalizeText(a))) errors.Add($"accepted answer '{a}' is not normalized");
    }

    private static void ValidateMapping(JsonElement content, JsonElement answer, string itemsKey, string targetsKey, List<string> errors)
    {
        var items = Ids(content, itemsKey, errors);
        var targets = Ids(content, targetsKey, errors);
        var mapping = Object(answer, "mapping");
        if (mapping is null) { errors.Add("answer.mapping must be an object"); return; }
        if (!items.SetEquals(mapping.Keys)) errors.Add($"answer.mapping must map every {itemsKey} id");
        if (!mapping.Values.All(targets.Contains)) errors.Add($"answer.mapping values must reference {targetsKey}");
    }

    private static int? ShortestPath(List<string> grid, List<string> errors)
    {
        if (grid.Count == 0 || grid.Any(r => r.Length != grid[0].Length || r.Any(c => c is not ('S' or 'E' or '#' or '.'))))
        {
            errors.Add("grid must be rectangular and use only S, E, # and .");
            return null;
        }

        var cells = grid.SelectMany((row, r) => row.Select((ch, c) => (ch, r, c))).ToList();
        if (cells.Count(x => x.ch == 'S') != 1 || cells.Count(x => x.ch == 'E') != 1)
        {
            errors.Add("grid must contain exactly one S and one E");
            return null;
        }

        var start = cells.Single(x => x.ch == 'S');
        var queue = new Queue<(int R, int C, int D)>([(start.r, start.c, 0)]);
        var seen = new HashSet<(int, int)> { (start.r, start.c) };
        while (queue.TryDequeue(out var cur))
        {
            if (grid[cur.R][cur.C] == 'E') return cur.D;
            foreach (var (dr, dc) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var (nr, nc) = (cur.R + dr, cur.C + dc);
                if (nr >= 0 && nr < grid.Count && nc >= 0 && nc < grid[0].Length && grid[nr][nc] != '#' && seen.Add((nr, nc)))
                {
                    queue.Enqueue((nr, nc, cur.D + 1));
                }
            }
        }

        return null;
    }

    private static decimal Evaluate(string expression)
    {
        var normalized = expression.Replace('×', '*').Replace('÷', '/');
        return Convert.ToDecimal(new DataTable().Compute(normalized, null), CultureInfo.InvariantCulture);
    }

    private static JsonElement? Prop(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : null;

    private static string? Str(JsonElement element, string name) =>
        Prop(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static IEnumerable<JsonElement> Array(JsonElement element, string name) =>
        Prop(element, name) is { ValueKind: JsonValueKind.Array } value ? value.EnumerateArray() : [];

    private static Dictionary<string, string>? Object(JsonElement element, string name) =>
        Prop(element, name) is { ValueKind: JsonValueKind.Object } value
            ? value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ValueKind == JsonValueKind.Number ? p.Value.GetRawText() : p.Value.GetString()!)
            : null;

    private static void RequireString(JsonElement element, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(Str(element, name))) errors.Add($"'{name}' is required");
    }

    private static HashSet<string> Ids(JsonElement content, string key, List<string> errors)
    {
        var list = Array(content, key).ToList();
        if (list.Count == 0) errors.Add($"'{key}' must be a non-empty array");
        var ids = list.Select(e => Str(e, "id") ?? "").ToList();
        if (ids.Any(string.IsNullOrEmpty) || ids.Distinct().Count() != ids.Count) errors.Add($"'{key}' ids must be present and unique");
        // Chaque élément affiché doit avoir un libellé : "text" en général, "face" pour une carte, "shape" pour une forme.
        if (list.Any(e => Str(e, "text") is null && Str(e, "face") is null && Str(e, "shape") is null))
        {
            errors.Add($"'{key}' entries need a text");
        }

        return ids.ToHashSet();
    }
}
