using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Tests.Seed;

/// <summary>Ensures the format checker really detects errors (it must not pass vacuously).</summary>
public class ChallengeFormatRulesTests
{
    private static List<string> Check(ChallengeType type, string content, string answer) =>
        ChallengeFormatRules.Validate(type, JsonDocument.Parse(content).RootElement, JsonDocument.Parse(answer).RootElement);

    [Fact]
    public void Wrong_maze_length_is_detected()
    {
        var errors = Check(ChallengeType.Maze, """{"prompt":"p","grid":["S.","#E"]}""", """{"shortestPathLength":3}""");
        Assert.Contains(errors, e => e.Contains("BFS finds 2"));
    }

    [Fact]
    public void Unsolvable_maze_is_detected()
    {
        var errors = Check(ChallengeType.Maze, """{"prompt":"p","grid":["S#","#E"]}""", """{"shortestPathLength":2}""");
        Assert.Contains(errors, e => e.Contains("no path"));
    }

    [Fact]
    public void Wrong_math_result_is_detected_and_priorities_are_respected()
    {
        var errors = Check(ChallengeType.MathMiniGame,
            """{"prompt":"p","problems":[{"id":"p1","expression":"48 + 52 × 2"}]}""",
            """{"answers":{"p1":200}}""");
        Assert.Contains(errors, e => e.Contains("= 152"));
    }

    [Fact]
    public void Answer_referencing_an_unknown_option_is_detected()
    {
        var errors = Check(ChallengeType.MultipleChoice,
            """{"prompt":"p","options":[{"id":"a","text":"A"},{"id":"b","text":"B"}]}""",
            """{"optionId":"z"}""");
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Ordering_items_shown_in_the_correct_order_are_detected()
    {
        var errors = Check(ChallengeType.OrderingClassification,
            """{"prompt":"p","mode":"order","items":[{"id":"i1","text":"1"},{"id":"i2","text":"2"}]}""",
            """{"order":["i1","i2"]}""");
        Assert.Contains(errors, e => e.Contains("given away"));
    }

    [Theory]
    [InlineData("  La Planète   MARS ", "la planete mars")]
    [InlineData("Toutânkhamon", "toutankhamon")]
    [InlineData("H₂O", "h2o")]
    public void Text_answers_are_normalized(string raw, string expected)
    {
        Assert.Equal(expected, ChallengeFormatRules.NormalizeText(raw));
    }
}
