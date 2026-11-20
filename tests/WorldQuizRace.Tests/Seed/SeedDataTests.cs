using System.Text;
using System.Text.RegularExpressions;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;
using WorldQuizRace.Domain.Map;
using WorldQuizRace.Infrastructure.Seed;
using Xunit.Abstractions;

namespace WorldQuizRace.Tests.Seed;

/// <summary>Checks the embedded seed files, without a database.</summary>
public partial class SeedDataTests(ITestOutputHelper output)
{
    private const int CheckpointsPerRoute = 15;
    private const int MinimumPerDifficulty = 15;

    private static readonly RouteSeedFile Routes = SeedDataReader.ReadRoutes();
    private static readonly IReadOnlyList<ChallengeSeed> Challenges = SeedDataReader.ReadChallenges();

    // ---------- Routes ----------

    [Fact]
    public void Routes_pass_seeder_validation()
    {
        DatabaseSeeder.ValidateRoutes(Routes, CheckpointsPerRoute);
    }

    [Fact]
    public void All_routes_have_the_same_number_of_checkpoints()
    {
        Assert.True(Routes.Routes.Count >= 2);
        Assert.All(Routes.Routes, r => Assert.Equal(CheckpointsPerRoute - 1, r.Checkpoints.Count));
    }

    [Fact]
    public void Checkpoint_positions_are_contiguous_without_gaps()
    {
        Assert.All(Routes.Routes, r =>
            Assert.Equal(Enumerable.Range(1, CheckpointsPerRoute - 1), r.Checkpoints.Select(c => c.Position)));
    }

    [Fact]
    public void Routes_start_at_opposite_ends_and_head_towards_the_shared_destination()
    {
        var west = Routes.Routes.Single(r => r.Code == "WEST");
        var east = Routes.Routes.Single(r => r.Code == "EAST");
        var destinationX = MapProjection.ToMap(Routes.Destination.Latitude, Routes.Destination.Longitude).X;

        var westStartX = MapProjection.ToMap(west.Checkpoints[0].Latitude, west.Checkpoints[0].Longitude).X;
        var eastStartX = MapProjection.ToMap(east.Checkpoints[0].Latitude, east.Checkpoints[0].Longitude).X;

        Assert.True(westStartX < 150, "Route A must start at the far west of the map.");
        Assert.True(eastStartX > 850, "Route B must start at the far east of the map.");
        Assert.InRange(destinationX, westStartX, eastStartX);
    }

    [Fact]
    public void Routes_have_comparable_visual_lengths()
    {
        var lengths = Routes.Routes.Select(r =>
        {
            var points = r.Checkpoints.Select(c => MapProjection.ToMap(c.Latitude, c.Longitude))
                .Append(MapProjection.ToMap(Routes.Destination.Latitude, Routes.Destination.Longitude))
                .ToList();
            return points.Zip(points.Skip(1), (a, b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2))).Sum();
        }).ToList();

        output.WriteLine($"Route lengths (viewBox units): {string.Join(" / ", lengths.Select(l => l.ToString("F1")))}");
        Assert.True(lengths.Max() / lengths.Min() < 1.10, "Route visual lengths must differ by less than 10 %.");
    }

    [Fact]
    public void Checkpoint_texts_fit_the_database_columns()
    {
        var stops = Routes.Routes.SelectMany(r => r.Checkpoints)
            .Select(c => (c.Name, c.Country, c.FunFact))
            .Append((Routes.Destination.Name, Routes.Destination.Country, Routes.Destination.FunFact));

        Assert.All(stops, s =>
        {
            Assert.InRange(s.Name.Length, 1, Checkpoint.NameMaxLength);
            Assert.InRange(s.Country.Length, 1, Checkpoint.CountryMaxLength);
            Assert.InRange(s.FunFact.Length, 1, Checkpoint.FunFactMaxLength);
        });
    }

    [Theory]
    [InlineData(90, -180, 0, 0)]
    [InlineData(-90, 180, 1000, 500)]
    [InlineData(0, 0, 500, 250)]
    [InlineData(30.04, 31.24, 586.8, 166.6)]
    public void Map_projection_is_equirectangular_in_a_1000x500_viewbox(double lat, double lon, double x, double y)
    {
        Assert.Equal((x, y), MapProjection.ToMap(lat, lon));
    }

    // ---------- Challenges ----------

    [Fact]
    public void Each_age_group_has_enough_challenges_per_difficulty()
    {
        foreach (var minAge in ReferenceData.AgeGroups.Select(a => a.MinimumAge))
        {
            foreach (var difficulty in Enum.GetValues<Difficulty>())
            {
                var count = Challenges.Count(c => c.MinAge == minAge && c.Difficulty == difficulty);
                Assert.True(count >= MinimumPerDifficulty, $"Age {minAge}+ / {difficulty}: {count} challenge(s), {MinimumPerDifficulty} expected.");
            }
        }
    }

    [Fact]
    public void Each_age_group_covers_several_challenge_types()
    {
        foreach (var group in Challenges.GroupBy(c => c.MinAge))
        {
            var types = group.Select(c => c.Type).Distinct().Count();
            var multipleChoiceShare = group.Count(c => c.Type == ChallengeType.MultipleChoice) / (double)group.Count();
            Assert.True(types >= 8, $"Age {group.Key}+ uses only {types} challenge types.");
            Assert.True(multipleChoiceShare < 0.3, $"Age {group.Key}+ relies too much on multiple choice.");
        }
    }

    [Fact]
    public void Challenge_codes_are_unique_and_match_their_age_group_and_difficulty()
    {
        Assert.Equal(Challenges.Count, Challenges.Select(c => c.Code).Distinct().Count());
        var groupIndex = ReferenceData.AgeGroups.Select((a, i) => (a.MinimumAge, Index: i + 1)).ToDictionary(x => x.MinimumAge, x => x.Index);

        Assert.All(Challenges, c =>
        {
            var match = CodePattern().Match(c.Code);
            Assert.True(match.Success, $"Bad code format: {c.Code}");
            Assert.Equal(groupIndex[c.MinAge].ToString(), match.Groups["group"].Value);
            Assert.Equal(c.Difficulty.ToString()[0].ToString(), match.Groups["difficulty"].Value);
        });
    }

    [Fact]
    public void Challenge_references_and_lengths_are_valid()
    {
        var categories = ReferenceData.Categories.Select(c => c.Name).ToHashSet();
        var ages = ReferenceData.AgeGroups.Select(a => a.MinimumAge).ToHashSet();

        Assert.All(Challenges, c =>
        {
            Assert.Contains(c.Category, categories);
            Assert.Contains(c.MinAge, ages);
            Assert.InRange(c.Title.Length, 1, Challenge.TitleMaxLength);
            Assert.InRange(c.Explanation.Length, 1, Challenge.ExplanationMaxLength);
            if (c.TimeLimitSeconds is { } limit) Assert.InRange(limit, 5, 600);
        });
    }

    [Fact]
    public void Every_challenge_content_and_answer_match_the_format_of_its_type()
    {
        var failures = Challenges
            .Select(c => (c.Code, Errors: ChallengeFormatRules.Validate(c.Type, c.Content, c.CorrectAnswer)))
            .Where(r => r.Errors.Count > 0)
            .Select(r => $"{r.Code}: {string.Join("; ", r.Errors)}")
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void Public_content_never_contains_the_answer_keys()
    {
        string[] secretKeys = ["optionId", "acceptedAnswers", "mapping", "pairs", "selectedIds", "shortestPathLength", "answers", "order", "values"];
        Assert.All(Challenges, c =>
        {
            foreach (var key in secretKeys)
            {
                Assert.False(c.Content.TryGetProperty(key, out _), $"{c.Code}: content exposes '{key}'.");
            }
        });
    }

    [Fact]
    public void Print_challenge_count_table()
    {
        var types = Enum.GetValues<ChallengeType>();
        var sb = new StringBuilder();
        sb.AppendLine($"{"Tranche",-10}{"Diff.",-8}" + string.Concat(types.Select(t => $"{Abbreviation(t),5}")) + "  Total");
        foreach (var age in ReferenceData.AgeGroups)
        {
            foreach (var difficulty in Enum.GetValues<Difficulty>())
            {
                var set = Challenges.Where(c => c.MinAge == age.MinimumAge && c.Difficulty == difficulty).ToList();
                sb.AppendLine($"{age.Name,-10}{difficulty,-8}" + string.Concat(types.Select(t => $"{set.Count(c => c.Type == t),5}")) + $"  {set.Count,5}");
            }
        }

        sb.AppendLine($"Total : {Challenges.Count}");
        output.WriteLine(sb.ToString());
        Assert.Equal(180, Challenges.Count);
    }

    private static string Abbreviation(ChallengeType type) => type switch
    {
        ChallengeType.MultipleChoice => "QCM",
        ChallengeType.TrueFalse => "V/F",
        ChallengeType.TextAnswer => "Txt",
        ChallengeType.NumericAnswer => "Num",
        ChallengeType.DragAndDrop => "D&D",
        ChallengeType.ShapeSorting => "Frm",
        ChallengeType.ImageMatching => "Img",
        ChallengeType.MemoryCards => "Mém",
        ChallengeType.SequenceCompletion => "Seq",
        ChallengeType.LogicPuzzle => "Log",
        ChallengeType.Maze => "Lab",
        ChallengeType.OrderingClassification => "Ord",
        ChallengeType.MathMiniGame => "Mini",
        ChallengeType.VisualChallenge => "Vis",
        _ => type.ToString()
    };

    [GeneratedRegex(@"^G(?<group>[1-4])-(?<difficulty>[EMH])-\d{2}$")]
    private static partial Regex CodePattern();
}
