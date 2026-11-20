using Microsoft.Extensions.Configuration;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Tests.Application;

public class GameSettingsValidatorTests
{
    private readonly GameSettingsValidator _validator = new();

    private static GameSettings ValidSettings() => new()
    {
        CheckpointsPerRoute = 15,
        TurnMode = TurnMode.Alternating,
        FirstPlayer = PlayerSlot.A,
        DifficultyRules =
        [
            new() { FromPosition = 1, ToPosition = 5, Difficulty = Difficulty.Easy },
            new() { FromPosition = 6, ToPosition = 10, Difficulty = Difficulty.Medium },
            new() { FromPosition = 11, ToPosition = 15, Difficulty = Difficulty.Hard }
        ],
        DefaultTimeLimitsSeconds = Enum.GetValues<ChallengeType>().ToDictionary(t => t, _ => 30)
    };

    [Fact]
    public void Valid_settings_pass()
    {
        Assert.True(_validator.Validate(null, ValidSettings()).Succeeded);
    }

    [Fact]
    public void Web_appsettings_json_is_valid()
    {
        var webAppSettings = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var settings = new ConfigurationBuilder()
            .AddJsonFile(webAppSettings, optional: false)
            .Build()
            .GetSection(GameSettings.SectionName)
            .Get<GameSettings>();

        Assert.NotNull(settings);
        var result = _validator.Validate(null, settings);
        Assert.True(result.Succeeded, result.FailureMessage);
        Assert.Equal(15, settings.CheckpointsPerRoute);
    }

    [Fact]
    public void Gap_in_difficulty_rules_fails()
    {
        var settings = ValidSettings();
        settings.DifficultyRules[1].ToPosition = 9; // position 10 n'est plus couverte

        var result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("position 10", result.FailureMessage);
    }

    [Fact]
    public void Overlapping_difficulty_rules_fail()
    {
        var settings = ValidSettings();
        settings.DifficultyRules[0].ToPosition = 6; // position 6 couverte deux fois

        Assert.True(_validator.Validate(null, settings).Failed);
    }

    [Fact]
    public void Missing_time_limit_fails()
    {
        var settings = ValidSettings();
        settings.DefaultTimeLimitsSeconds.Remove(ChallengeType.Maze);

        var result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains(nameof(ChallengeType.Maze), result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(51)]
    public void Out_of_range_checkpoint_count_fails(int checkpoints)
    {
        var settings = ValidSettings();
        settings.CheckpointsPerRoute = checkpoints;

        Assert.True(_validator.Validate(null, settings).Failed);
    }
}
