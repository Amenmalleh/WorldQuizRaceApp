using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Answers.Validators;

/// <summary>
/// Submission: {"moves":"RDDDRR"} (U, D, L, R; an array of moves is accepted too).
/// The server replays the moves from S: every step goes to an adjacent cell inside the grid,
/// never into a wall, and the path must end on E. Any valid path is accepted, not only the shortest.
/// </summary>
public sealed class MazeValidator : IAnswerValidator
{
    public ChallengeType Type => ChallengeType.Maze;

    public bool Validate(JsonElement content, JsonElement correctAnswer, JsonElement submitted)
    {
        var grid = AnswerReader.StringList(content, "grid");
        var moves = ReadMoves(submitted);
        if (grid is not { Count: > 0 } || moves is null || grid.Any(row => row.Length != grid[0].Length))
        {
            return false;
        }

        // A path never needs to visit every cell more than a few times: reject absurdly long inputs.
        if (moves.Count == 0 || moves.Count > grid.Count * grid[0].Length * 4)
        {
            return false;
        }

        var start = Find(grid, 'S');
        if (start is null || Find(grid, 'E') is null)
        {
            return false;
        }

        var (row, col) = start.Value;
        foreach (var move in moves)
        {
            (row, col) = move switch
            {
                'U' => (row - 1, col),
                'D' => (row + 1, col),
                'L' => (row, col - 1),
                'R' => (row, col + 1),
                _ => (-1, -1)
            };

            if (row < 0 || row >= grid.Count || col < 0 || col >= grid[0].Length || grid[row][col] == '#')
            {
                return false;
            }
        }

        return grid[row][col] == 'E';
    }

    private static List<char>? ReadMoves(JsonElement submitted)
    {
        var raw = AnswerReader.Property(submitted, "moves") switch
        {
            { ValueKind: JsonValueKind.String } text => text.GetString(),
            { ValueKind: JsonValueKind.Array } array => string.Concat(array.EnumerateArray()
                .Select(m => m.ValueKind == JsonValueKind.String ? m.GetString() : "?")),
            _ => null
        };

        return raw?.Where(c => !char.IsWhiteSpace(c) && c != ',').Select(char.ToUpperInvariant).ToList();
    }

    private static (int Row, int Col)? Find(List<string> grid, char target)
    {
        for (var r = 0; r < grid.Count; r++)
        {
            var c = grid[r].IndexOf(target);
            if (c >= 0)
            {
                return (r, c);
            }
        }

        return null;
    }
}
