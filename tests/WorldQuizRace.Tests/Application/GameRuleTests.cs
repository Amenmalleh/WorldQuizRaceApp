using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Application.Services.Challenges;
using WorldQuizRace.Application.Services.Players;
using WorldQuizRace.Application.Services.Progression;
using WorldQuizRace.Application.Services.Rewards;
using WorldQuizRace.Application.Services.Turns;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Tests.Application;

/// <summary>Pure unit tests of the game rules (no database).</summary>
public class GameRuleTests
{
    private const int Total = 15;
    private readonly ProgressionService _progression = new();
    private readonly WinnerService _winner = new();
    private readonly TurnService _turns = new([new AlternatingTurnStrategy()]);

    private static Match NewMatch()
    {
        var match = new Match { Id = 1, Status = MatchStatus.InProgress, TurnMode = TurnMode.Alternating, TurnNumber = 1, CurrentTurnPlayerId = 10 };
        match.Players.Add(new MatchPlayer { PlayerId = 10, Slot = PlayerSlot.A });
        match.Players.Add(new MatchPlayer { PlayerId = 20, Slot = PlayerSlot.B });
        return match;
    }

    // ---------- Progression ----------

    [Fact]
    public void Correct_answer_moves_exactly_one_checkpoint()
    {
        var participant = new MatchPlayer { CurrentPosition = 4 };
        var result = _progression.Apply(participant, isCorrect: true, Total);

        Assert.Equal((4, 5), (result.PreviousPosition, result.NewPosition));
        Assert.Equal((1, 1, 0), (participant.Attempts, participant.CorrectAnswers, participant.WrongAnswers));
    }

    [Fact]
    public void Wrong_answer_never_moves_the_player_and_never_goes_back()
    {
        var participant = new MatchPlayer { CurrentPosition = 7 };
        for (var i = 0; i < 5; i++)
        {
            _progression.Apply(participant, isCorrect: false, Total);
        }

        Assert.Equal(7, participant.CurrentPosition);
        Assert.Equal((5, 0, 5), (participant.Attempts, participant.CorrectAnswers, participant.WrongAnswers));
    }

    [Fact]
    public void Nobody_moves_past_the_final_checkpoint()
    {
        var participant = new MatchPlayer { CurrentPosition = Total - 1 };
        Assert.Equal(Total, _progression.Apply(participant, true, Total).NewPosition);
        Assert.Throws<InvalidOperationException>(() => _progression.Apply(participant, true, Total));
        Assert.Equal(Total, participant.CurrentPosition);
    }

    // ---------- Winner ----------

    [Fact]
    public void Reaching_the_final_checkpoint_wins_and_closes_the_match()
    {
        var match = NewMatch();
        var participant = match.Players.First();
        var now = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

        participant.CurrentPosition = Total - 1;
        Assert.False(_winner.TryDeclareWinner(match, participant, Total, now));

        participant.CurrentPosition = Total;
        Assert.True(_winner.TryDeclareWinner(match, participant, Total, now));
        Assert.Equal((MatchStatus.Finished, (int?)10, (DateTimeOffset?)now, (int?)null),
            (match.Status, match.WinnerId, match.EndDate, match.CurrentTurnPlayerId));

        // A second player arriving later cannot become a second winner.
        Assert.False(_winner.TryDeclareWinner(match, match.Players.Last(), Total, now));
    }

    // ---------- Turns ----------

    [Fact]
    public void Alternating_turns_go_A_B_A_B()
    {
        var match = NewMatch();
        var sequence = new List<int?> { match.CurrentTurnPlayerId };
        for (var i = 0; i < 3; i++)
        {
            _turns.AdvanceTurn(match);
            sequence.Add(match.CurrentTurnPlayerId);
        }

        Assert.Equal([10, 20, 10, 20], sequence);
        Assert.Equal(4, match.TurnNumber);
        Assert.True(_turns.IsPlayersTurn(match, 20));
        Assert.False(_turns.IsPlayersTurn(match, 10));
    }

    // ---------- Difficulty ----------

    private static DifficultyPolicy Policy() => new(Microsoft.Extensions.Options.Options.Create(new GameSettings
    {
        DifficultyRules =
        [
            new() { FromPosition = 1, ToPosition = 5, Difficulty = Difficulty.Easy },
            new() { FromPosition = 6, ToPosition = 10, Difficulty = Difficulty.Medium },
            new() { FromPosition = 11, ToPosition = 15, Difficulty = Difficulty.Hard }
        ]
    }));

    [Theory]
    [InlineData(1, Difficulty.Easy)]
    [InlineData(5, Difficulty.Easy)]
    [InlineData(6, Difficulty.Medium)]
    [InlineData(10, Difficulty.Medium)]
    [InlineData(11, Difficulty.Hard)]
    [InlineData(15, Difficulty.Hard)]
    public void Difficulty_depends_on_the_position_only(int position, Difficulty expected)
    {
        Assert.Equal(expected, Policy().ForPosition(position));
    }

    [Theory]
    [InlineData(Difficulty.Easy, new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })]
    [InlineData(Difficulty.Medium, new[] { Difficulty.Medium, Difficulty.Easy, Difficulty.Hard })]
    [InlineData(Difficulty.Hard, new[] { Difficulty.Hard, Difficulty.Medium, Difficulty.Easy })]
    public void Fallback_goes_to_the_closest_difficulty_easier_first(Difficulty target, Difficulty[] expected)
    {
        Assert.Equal(expected, Policy().FallbackOrder(target));
    }

    // ---------- Badges ----------

    [Fact]
    public void Badge_rules_follow_their_definition()
    {
        var start = new BadgeContext(Position: 1, Total, CorrectAnswers: 1, RecentResults: [true], MatchFinished: false);
        var streak = start with { Position = 3, CorrectAnswers = 3, RecentResults = [false, true, true, true] };
        var broken = streak with { RecentResults = [true, true, false, true] };

        Assert.True(new FirstStepBadgeRule().IsEarned(start));
        Assert.False(new FirstStepBadgeRule().IsEarned(start with { CorrectAnswers = 0 }));
        Assert.True(new ThreeInARowBadgeRule().IsEarned(streak));
        Assert.False(new ThreeInARowBadgeRule().IsEarned(broken));
        Assert.True(new HalfwayBadgeRule().IsEarned(start with { Position = 8 }));
        Assert.False(new HalfwayBadgeRule().IsEarned(start with { Position = 7 }));
        Assert.True(new WorldExplorerBadgeRule().IsEarned(start with { Position = Total }));
        Assert.True(new BraveLearnerBadgeRule().IsEarned(start with { MatchFinished = true }));
        Assert.False(new BraveLearnerBadgeRule().IsEarned(start));
    }

    // ---------- Nickname ----------

    [Theory]
    [InlineData("  Ad   am ", "Ad am")]
    [InlineData("Lina", "Lina")]
    [InlineData(null, "")]
    public void Nickname_whitespace_is_cleaned(string? raw, string expected)
    {
        Assert.Equal(expected, PlayerRegistrationService.NormalizeName(raw));
    }
}
