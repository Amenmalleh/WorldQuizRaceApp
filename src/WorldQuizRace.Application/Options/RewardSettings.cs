namespace WorldQuizRace.Application.Options;

/// <summary>Points, stars and coins granted by the reward service.</summary>
public sealed class RewardSettings
{
    public int PointsPerCorrectAnswer { get; set; } = 100;

    public int SpeedBonusPoints { get; set; } = 50;

    public int StarsPerCorrectAnswer { get; set; } = 1;

    public int SpeedBonusStars { get; set; } = 1;

    /// <summary>An answer earns the speed bonus when given within this fraction of the time limit (0.5 = first half).</summary>
    public double SpeedBonusMaxTimeRatio { get; set; } = 0.5;

    public int CoinsPerCorrectAnswer { get; set; } = 10;

    public int CoinsForReachingDestination { get; set; } = 50;
}
