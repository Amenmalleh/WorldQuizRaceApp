using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Tests.Support;

/// <summary>Builds submissions for tests: the correct answer of any challenge, or an obviously wrong one.</summary>
public static class AnswerFactory
{
    /// <summary>An answer of the wrong shape for every type: always judged wrong, never an error.</summary>
    public static JsonElement Wrong { get; } = JsonDocument.Parse("""{"nothing":"here"}""").RootElement.Clone();

    public static JsonElement Correct(ChallengeType type, string contentJson, string correctAnswerJson)
    {
        var content = JsonNode.Parse(contentJson)!.AsObject();
        var correct = JsonNode.Parse(correctAnswerJson)!.AsObject();

        JsonNode submission = type switch
        {
            ChallengeType.TextAnswer => TextOf(correct),
            ChallengeType.LogicPuzzle when (string?)content["answerType"] == "text" => TextOf(correct),
            ChallengeType.Maze => new JsonObject { ["moves"] = ShortestMoves(content["grid"]!.AsArray().Select(r => (string)r!).ToList()) },
            // Every other submission format is identical to the CorrectAnswer format (docs/CHALLENGE_FORMATS.md).
            _ => correct
        };

        return JsonDocument.Parse(submission.ToJsonString()).RootElement.Clone();
    }

    public static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static JsonObject TextOf(JsonObject correct) =>
        new() { ["text"] = (string?)correct["acceptedAnswers"]![0] };

    /// <summary>Breadth-first search returning the shortest U/D/L/R path from S to E.</summary>
    public static string ShortestMoves(IReadOnlyList<string> grid)
    {
        var start = Find(grid, 'S');
        var previous = new Dictionary<(int, int), ((int, int) From, char Move)>();
        var queue = new Queue<(int R, int C)>([start]);
        var seen = new HashSet<(int, int)> { start };
        (char Move, int Dr, int Dc)[] steps = [('U', -1, 0), ('D', 1, 0), ('L', 0, -1), ('R', 0, 1)];

        while (queue.TryDequeue(out var cell))
        {
            if (grid[cell.R][cell.C] == 'E')
            {
                var path = new StringBuilder();
                var current = cell;
                while (current != start)
                {
                    var (from, move) = previous[current];
                    path.Insert(0, move);
                    current = from;
                }

                return path.ToString();
            }

            foreach (var (move, dr, dc) in steps)
            {
                var next = (R: cell.R + dr, C: cell.C + dc);
                if (next.R >= 0 && next.R < grid.Count && next.C >= 0 && next.C < grid[0].Length
                    && grid[next.R][next.C] != '#' && seen.Add(next))
                {
                    previous[next] = (cell, move);
                    queue.Enqueue(next);
                }
            }
        }

        throw new InvalidOperationException("Maze without solution.");
    }

    private static (int R, int C) Find(IReadOnlyList<string> grid, char target)
    {
        for (var r = 0; r < grid.Count; r++)
        {
            var c = grid[r].IndexOf(target);
            if (c >= 0) return (r, c);
        }

        throw new InvalidOperationException($"'{target}' not found in maze.");
    }
}
