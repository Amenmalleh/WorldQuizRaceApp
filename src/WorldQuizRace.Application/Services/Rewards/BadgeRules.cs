namespace WorldQuizRace.Application.Services.Rewards;

/// <summary>Everything a badge rule may look at, for one player, right after an answer.</summary>
/// <param name="RecentResults">Results of this player's resolved attempts in the match, oldest first, current one last.</param>
public sealed record BadgeContext(
    int Position,
    int TotalCheckpoints,
    int CorrectAnswers,
    IReadOnlyList<bool> RecentResults,
    bool MatchFinished);

/// <summary>
/// One unlockable badge. Rules are discovered automatically: adding a badge means adding a rule class
/// and the badge row (code, name, icon) in the seed reference data.
/// </summary>
public interface IBadgeRule
{
    string BadgeCode { get; }

    bool IsEarned(BadgeContext context);
}

public static class BadgeCodes
{
    public const string FirstStep = "FIRST_STEP";
    public const string ThreeInARow = "STREAK_3";
    public const string Halfway = "HALFWAY";
    public const string WorldExplorer = "WORLD_EXPLORER";
    public const string BraveLearner = "BRAVE_LEARNER";
}

/// <summary>« Premier pas » : first challenge solved.</summary>
public sealed class FirstStepBadgeRule : IBadgeRule
{
    public string BadgeCode => BadgeCodes.FirstStep;

    public bool IsEarned(BadgeContext context) => context.CorrectAnswers >= 1;
}

/// <summary>« 3 sans faute » : three correct answers in a row.</summary>
public sealed class ThreeInARowBadgeRule : IBadgeRule
{
    public const int StreakLength = 3;

    public string BadgeCode => BadgeCodes.ThreeInARow;

    public bool IsEarned(BadgeContext context) =>
        context.RecentResults.Count >= StreakLength && context.RecentResults.TakeLast(StreakLength).All(correct => correct);
}

/// <summary>« Mi-parcours » : half of the route travelled.</summary>
public sealed class HalfwayBadgeRule : IBadgeRule
{
    public string BadgeCode => BadgeCodes.Halfway;

    public bool IsEarned(BadgeContext context) => context.Position * 2 >= context.TotalCheckpoints;
}

/// <summary>« Explorateur du monde » : final destination reached.</summary>
public sealed class WorldExplorerBadgeRule : IBadgeRule
{
    public string BadgeCode => BadgeCodes.WorldExplorer;

    public bool IsEarned(BadgeContext context) => context.Position >= context.TotalCheckpoints;
}

/// <summary>« Apprenti courageux » : played a match until the end, won or not (keeps the loser motivated).</summary>
public sealed class BraveLearnerBadgeRule : IBadgeRule
{
    public string BadgeCode => BadgeCodes.BraveLearner;

    public bool IsEarned(BadgeContext context) => context.MatchFinished;
}
