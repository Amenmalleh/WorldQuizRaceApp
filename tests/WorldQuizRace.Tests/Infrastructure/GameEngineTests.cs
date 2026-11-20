using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Exceptions;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Application.Services.Challenges;
using WorldQuizRace.Application.Services.Players;
using WorldQuizRace.Domain.Enums;
using WorldQuizRace.Tests.Support;

namespace WorldQuizRace.Tests.Infrastructure;

/// <summary>Integration tests of the game engine against the throwaway LocalDB database.</summary>
[Collection(LocalDbCollection.Name)]
public class GameEngineTests(LocalDbFixture fixture)
{
    // ---------- Registration & age groups ----------

    [Theory]
    [InlineData(5, 5, 7)]
    [InlineData(7, 5, 7)]
    [InlineData(8, 8, 10)]
    [InlineData(10, 8, 10)]
    [InlineData(11, 11, 12)]
    [InlineData(12, 11, 12)]
    [InlineData(13, 13, 15)]
    [InlineData(15, 13, 15)]
    public async Task Age_is_mapped_to_its_age_group(int age, int min, int max)
    {
        await using var host = new GameTestHost(fixture);
        var group = await host.WithServiceAsync<IAgeGroupService, AgeGroupDto>(s => s.GetForAgeAsync(age));
        Assert.Equal((min, max), (group.MinimumAge, group.MaximumAge));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    public async Task Ages_outside_5_to_15_are_rejected(int age)
    {
        await using var host = new GameTestHost(fixture);
        var ex = await Assert.ThrowsAsync<GameValidationException>(() =>
            host.WithServiceAsync<IPlayerRegistrationService, PlayerDto>(s => s.RegisterAsync(new RegisterPlayerRequest("Zoé", age, null))));
        Assert.Contains(AgeGroupService.AgeField, ex.Errors.Keys);
    }

    [Fact]
    public async Task Registration_cleans_the_nickname_and_validates_name_and_avatar()
    {
        await using var host = new GameTestHost(fixture);
        var player = await host.WithServiceAsync<IPlayerRegistrationService, PlayerDto>(s => s.RegisterAsync(new RegisterPlayerRequest("  Ma   rie ", 9, "Owl")));
        Assert.Equal(("Ma rie", "owl", "8–10 ans"), (player.Name, player.Avatar, player.AgeGroupName));

        var ex = await Assert.ThrowsAsync<GameValidationException>(() =>
            host.WithServiceAsync<IPlayerRegistrationService, PlayerDto>(s => s.RegisterAsync(new RegisterPlayerRequest(" A ", 20, "dragon"))));
        Assert.Equal(["age", "avatar", "name"], ex.Errors.Keys.Order());

        await Assert.ThrowsAsync<GameValidationException>(() =>
            host.WithServiceAsync<IPlayerRegistrationService, PlayerDto>(s => s.RegisterAsync(new RegisterPlayerRequest(new string('x', 21), 9, null))));
        await Assert.ThrowsAsync<GameValidationException>(() =>
            host.WithServiceAsync<IPlayerRegistrationService, PlayerDto>(s => s.RegisterAsync(new RegisterPlayerRequest("<script>", 9, null))));
    }

    // ---------- Match creation ----------

    [Fact]
    public async Task New_match_puts_A_on_the_west_route_and_B_on_the_east_route_at_position_0()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();

        var a = state.Players.Single(p => p.Slot == PlayerSlot.A);
        var b = state.Players.Single(p => p.Slot == PlayerSlot.B);
        Assert.Equal((MatchStatus.InProgress, 1, 15), (state.Status, state.TurnNumber, state.TotalCheckpoints));
        Assert.Equal(("WEST", "EAST"), (a.Route.Code, b.Route.Code));
        Assert.Equal((0, 0), (a.Position, b.Position));
        Assert.Equal(a.Player.Id, state.CurrentTurnPlayerId);
        Assert.Equal(15, a.Route.Checkpoints.Count);
        Assert.Equal(a.Route.Checkpoints[^1].Name, b.Route.Checkpoints[^1].Name);
    }

    [Fact]
    public async Task First_player_can_be_random()
    {
        await using var host = new GameTestHost(fixture, s => s.FirstPlayer = FirstPlayerOption.Random);
        host.Random.Enqueue(1);
        var state = await host.CreateMatchAsync();
        Assert.Equal(PlayerSlot.B, state.CurrentTurnSlot);
    }

    [Fact]
    public async Task The_same_player_cannot_play_against_themself()
    {
        await using var host = new GameTestHost(fixture);
        var player = await host.WithServiceAsync<IPlayerRegistrationService, PlayerDto>(s => s.RegisterAsync(new RegisterPlayerRequest("Solo", 9, null)));
        await Assert.ThrowsAsync<GameValidationException>(() =>
            host.WithServiceAsync<WorldQuizRace.Application.Services.Matches.IMatchService, int>(s => s.CreateMatchAsync(player.Id, player.Id)));
    }

    [Fact]
    public async Task Invalid_registrations_are_all_reported_and_nothing_is_created()
    {
        await using var host = new GameTestHost(fixture);
        var before = await host.QueryAsync(db => db.Players.CountAsync());

        var ex = await Assert.ThrowsAsync<GameValidationException>(() => host.RunAsync(g => g.CreateMatchAsync(
            new CreateMatchRequest(new RegisterPlayerRequest("Al", 4, null), new RegisterPlayerRequest("", 9, null)))));

        Assert.Contains("playerA.age", ex.Errors.Keys);
        Assert.Contains("playerB.name", ex.Errors.Keys);
        Assert.Equal(before, await host.QueryAsync(db => db.Players.CountAsync()));
    }

    // ---------- Challenge selection ----------

    [Fact]
    public async Task Each_player_only_receives_challenges_of_their_own_age_group()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync(ageA: 9, ageB: 13);
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        for (var turn = 0; turn < 8; turn++)
        {
            await host.PlayTurnAsync(state.MatchId, turn % 2 == 0 ? a : b, correct: turn % 4 < 2);
        }

        var groups = await host.QueryAsync(db => db.PlayerChallenges
            .Where(pc => pc.MatchId == state.MatchId)
            .Select(pc => new { pc.PlayerId, pc.Challenge.AgeGroup.MinimumAge })
            .ToListAsync());
        Assert.All(groups.Where(g => g.PlayerId == a), g => Assert.Equal(8, g.MinimumAge));
        Assert.All(groups.Where(g => g.PlayerId == b), g => Assert.Equal(13, g.MinimumAge));
    }

    [Fact]
    public async Task Both_players_get_the_same_relative_difficulty_at_the_same_position()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync(ageA: 6, ageB: 14);
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        var challengeA = await host.GetChallengeAsync(state.MatchId, a);
        await host.SubmitAsync(state.MatchId, challengeA.PlayerChallengeId, await host.CorrectAnswerAsync(challengeA.PlayerChallengeId));
        var challengeB = await host.GetChallengeAsync(state.MatchId, b);

        Assert.Equal((1, 1), (challengeA.CheckpointPosition, challengeB.CheckpointPosition));
        Assert.Equal((Difficulty.Easy, Difficulty.Easy), (challengeA.Difficulty, challengeB.Difficulty));
    }

    [Fact]
    public async Task No_challenge_is_repeated_until_the_stock_is_exhausted_then_fallback_applies()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync(ageA: 9, ageB: 9);
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        // Player A always answers wrong: they stay on position 1 (Easy). 15 Easy + 15 Medium + 15 Hard exist for 8–10.
        var servedToA = new List<ChallengeDto>();
        for (var i = 0; i < 47; i++)
        {
            var challenge = await host.GetChallengeAsync(state.MatchId, a);
            servedToA.Add(challenge);
            await host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, AnswerFactory.Wrong);
            host.Time.Advance(TimeSpan.FromSeconds(1));
            await host.PlayTurnAsync(state.MatchId, b, correct: false);
            host.Time.Advance(TimeSpan.FromSeconds(1));
        }

        var codes = await host.QueryAsync(db => db.PlayerChallenges
            .Where(pc => pc.MatchId == state.MatchId && pc.PlayerId == a)
            .OrderBy(pc => pc.Id)
            .Select(pc => pc.ChallengeId)
            .ToListAsync());

        // 1) The first 45 challenges are all different.
        Assert.Equal(45, codes.Take(45).Distinct().Count());
        // 2) Target difficulty first, then the closest one, then the farthest.
        Assert.All(servedToA.Take(15), c => Assert.Equal(Difficulty.Easy, c.Difficulty));
        Assert.All(servedToA.Skip(15).Take(15), c => Assert.Equal(Difficulty.Medium, c.Difficulty));
        Assert.All(servedToA.Skip(30).Take(15), c => Assert.Equal(Difficulty.Hard, c.Difficulty));
        // 3) Stock exhausted: the least recently seen challenge of the target difficulty comes back (first served, then second).
        Assert.Equal(codes[0], codes[45]);
        Assert.Equal(codes[1], codes[46]);
    }

    [Fact]
    public async Task Selection_reports_its_fallback_kind()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync(ageA: 11, ageB: 11);
        var a = state.Players[0].Player.Id;
        var ageGroupId = state.Players[0].Player.AgeGroupId;

        var selection = await host.WithServiceAsync<IChallengeSelectionService, ChallengeSelection>(s => s.SelectAsync(state.MatchId, a, ageGroupId, targetPosition: 12));
        Assert.Equal((Difficulty.Hard, ChallengeSelectionKind.Fresh), (selection.TargetDifficulty, selection.Kind));
        Assert.Equal(Difficulty.Hard, selection.Challenge.Difficulty);
    }

    // ---------- Progression, turns and integrity ----------

    [Fact]
    public async Task Correct_answer_advances_exactly_one_and_wrong_answer_does_not_move()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        var right = await host.PlayTurnAsync(state.MatchId, a, correct: true);
        Assert.True(right.IsCorrect);
        Assert.Equal((0, 1), (right.PreviousPosition, right.NewPosition));
        Assert.False(string.IsNullOrWhiteSpace(right.Explanation));

        var wrong = await host.PlayTurnAsync(state.MatchId, b, correct: false);
        Assert.False(wrong.IsCorrect);
        Assert.Equal((0, 0), (wrong.PreviousPosition, wrong.NewPosition));

        var wrongAgain = await host.PlayTurnAsync(state.MatchId, a, correct: false);
        Assert.Equal((1, 1), (wrongAgain.PreviousPosition, wrongAgain.NewPosition));
        var playerA = wrongAgain.State.Players.Single(p => p.Player.Id == a);
        Assert.Equal((2, 1, 1), (playerA.Attempts, playerA.CorrectAnswers, playerA.WrongAnswers));
    }

    [Fact]
    public async Task Turns_alternate_A_B_A_after_every_answer_correct_or_not()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        var turns = new List<int?> { state.CurrentTurnPlayerId };
        turns.Add((await host.PlayTurnAsync(state.MatchId, a, correct: true)).State.CurrentTurnPlayerId);
        turns.Add((await host.PlayTurnAsync(state.MatchId, b, correct: false)).State.CurrentTurnPlayerId);
        turns.Add((await host.PlayTurnAsync(state.MatchId, a, correct: false)).State.CurrentTurnPlayerId);

        Assert.Equal([a, b, a, b], turns);
    }

    [Fact]
    public async Task Reloading_the_challenge_returns_the_same_attempt()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var a = state.Players[0].Player.Id;

        var first = await host.GetChallengeAsync(state.MatchId, a);
        host.Time.Advance(TimeSpan.FromSeconds(5));
        var second = await host.GetChallengeAsync(state.MatchId, a);

        Assert.Equal(first.PlayerChallengeId, second.PlayerChallengeId);
        Assert.Equal(first.ServedAt, second.ServedAt);
        Assert.Equal(first.TimeLimitSeconds - 5, second.RemainingSeconds);
    }

    [Fact]
    public async Task Playing_out_of_turn_is_refused()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        var ex = await Assert.ThrowsAsync<GameRuleException>(() => host.GetChallengeAsync(state.MatchId, b));
        Assert.Equal(GameErrorCodes.NotYourTurn, ex.Code);

        await host.PlayTurnAsync(state.MatchId, a, correct: true);
        ex = await Assert.ThrowsAsync<GameRuleException>(() => host.GetChallengeAsync(state.MatchId, a));
        Assert.Equal(GameErrorCodes.NotYourTurn, ex.Code);
    }

    [Fact]
    public async Task Double_submission_moves_the_player_only_once()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var a = state.Players[0].Player.Id;

        var challenge = await host.GetChallengeAsync(state.MatchId, a);
        var answer = await host.CorrectAnswerAsync(challenge.PlayerChallengeId);
        var first = await host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, answer);
        var ex = await Assert.ThrowsAsync<GameRuleException>(() => host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, answer));

        Assert.Equal(GameErrorCodes.AttemptAlreadyAnswered, ex.Code);
        Assert.Equal(1, first.NewPosition);
        var after = await host.RunAsync(g => g.GetStateAsync(state.MatchId));
        Assert.Equal(1, after.Players.Single(p => p.Player.Id == a).Position);
        Assert.Equal(1, after.Players.Single(p => p.Player.Id == a).Attempts);
    }

    [Fact]
    public async Task Simultaneous_double_submission_moves_the_player_only_once()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var a = state.Players[0].Player.Id;
        var challenge = await host.GetChallengeAsync(state.MatchId, a);
        var answer = await host.CorrectAnswerAsync(challenge.PlayerChallengeId);

        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            try
            {
                await host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, answer);
                return true;
            }
            catch (GameException)
            {
                return false; // already answered, not your turn or concurrent update: all fine
            }
        }));

        Assert.Equal(1, attempts.Count(ok => ok));
        var after = await host.RunAsync(g => g.GetStateAsync(state.MatchId));
        var playerA = after.Players.Single(p => p.Player.Id == a);
        Assert.Equal((1, 1), (playerA.Position, playerA.Attempts));
    }

    [Fact]
    public async Task Submitting_an_attempt_of_another_match_is_refused()
    {
        await using var host = new GameTestHost(fixture);
        var first = await host.CreateMatchAsync();
        var second = await host.CreateMatchAsync();
        var challenge = await host.GetChallengeAsync(first.MatchId, first.Players[0].Player.Id);

        var ex = await Assert.ThrowsAsync<GameNotFoundException>(() =>
            host.SubmitAsync(second.MatchId, challenge.PlayerChallengeId, AnswerFactory.Wrong));
        Assert.Equal(GameErrorCodes.AttemptNotFound, ex.Code);
    }

    [Fact]
    public async Task Malformed_answer_is_a_wrong_answer_not_an_error()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var challenge = await host.GetChallengeAsync(state.MatchId, state.Players[0].Player.Id);

        var result = await host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, AnswerFactory.Parse("[1,2,{\"x\":null}]"));
        Assert.False(result.IsCorrect);
        Assert.Equal(0, result.NewPosition);
    }

    // ---------- Time limit ----------

    [Fact]
    public async Task Late_answer_is_timed_out_and_does_not_advance()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        var challenge = await host.GetChallengeAsync(state.MatchId, a);
        var answer = await host.CorrectAnswerAsync(challenge.PlayerChallengeId);
        host.Time.Advance(TimeSpan.FromSeconds(challenge.TimeLimitSeconds + 2.5));
        var result = await host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, answer);

        Assert.True(result.TimedOut);
        Assert.False(result.IsCorrect);
        Assert.Equal(0, result.NewPosition);
        Assert.Equal(b, result.State.CurrentTurnPlayerId);
        var status = await host.QueryAsync(db => db.PlayerChallenges.Where(pc => pc.Id == challenge.PlayerChallengeId).Select(pc => pc.Status).SingleAsync());
        Assert.Equal(AttemptStatus.TimedOut, status);
    }

    [Fact]
    public async Task Answer_within_the_grace_period_is_accepted()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var challenge = await host.GetChallengeAsync(state.MatchId, state.Players[0].Player.Id);
        var answer = await host.CorrectAnswerAsync(challenge.PlayerChallengeId);

        host.Time.Advance(TimeSpan.FromSeconds(challenge.TimeLimitSeconds + 1.5));
        var result = await host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, answer);

        Assert.False(result.TimedOut);
        Assert.True(result.IsCorrect);
    }

    // ---------- Rewards ----------

    [Fact]
    public async Task Correct_answers_earn_points_stars_coins_and_badges()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        var fast = await host.PlayTurnAsync(state.MatchId, a, correct: true);
        Assert.Equal((150, 2, 10), (fast.Reward.Points, fast.Reward.Stars, fast.Reward.Coins));
        Assert.Contains(fast.Reward.Badges, badge => badge.Code == "FIRST_STEP");

        await host.PlayTurnAsync(state.MatchId, b, correct: false);

        // Slow but correct answer: no speed bonus.
        var challenge = await host.GetChallengeAsync(state.MatchId, a);
        var answer = await host.CorrectAnswerAsync(challenge.PlayerChallengeId);
        host.Time.Advance(TimeSpan.FromSeconds(challenge.TimeLimitSeconds - 1));
        var slow = await host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, answer);
        Assert.Equal((100, 1, 10), (slow.Reward.Points, slow.Reward.Stars, slow.Reward.Coins));

        await host.PlayTurnAsync(state.MatchId, b, correct: false);
        var third = await host.PlayTurnAsync(state.MatchId, a, correct: true);
        Assert.Contains(third.Reward.Badges, badge => badge.Code == "STREAK_3");

        var wrong = await host.PlayTurnAsync(state.MatchId, b, correct: false);
        Assert.Equal(RewardDto.None.Points, wrong.Reward.Points);
    }

    // ---------- Challenge DTO never leaks the answer ----------

    [Fact]
    public async Task Serialized_challenge_never_contains_the_answer_or_the_explanation()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var a = state.Players[0].Player.Id;

        for (var i = 0; i < 6; i++)
        {
            var current = i % 2 == 0 ? a : state.Players[1].Player.Id;
            var challenge = await host.GetChallengeAsync(state.MatchId, current);
            var json = JsonSerializer.Serialize(challenge, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var secret = await host.QueryAsync(db => db.PlayerChallenges.Where(pc => pc.Id == challenge.PlayerChallengeId)
                .Select(pc => new { pc.Challenge.CorrectAnswer, pc.Challenge.Explanation }).SingleAsync());

            Assert.DoesNotContain("correctAnswer", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("explanation", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(secret.Explanation, json);
            Assert.DoesNotContain(secret.CorrectAnswer, json);
            foreach (var key in new[] { "\"optionId\"", "\"acceptedAnswers\"", "\"mapping\"", "\"pairs\"", "\"selectedIds\"", "\"shortestPathLength\"", "\"answers\"", "\"order\"", "\"values\"" })
            {
                Assert.DoesNotContain(key, json);
            }

            await host.SubmitAsync(state.MatchId, challenge.PlayerChallengeId, AnswerFactory.Wrong);
        }
    }

    // ---------- Victory and full game ----------

    [Fact]
    public async Task Full_game_until_victory_is_recorded_and_closes_the_match()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync(ageA: 9, ageB: 12);
        var (a, b) = (state.Players[0].Player.Id, state.Players[1].Player.Id);

        AnswerResultDto last = null!;
        var turn = 0;
        while (true)
        {
            // A answers everything right; B answers right one time out of three.
            var playerId = turn % 2 == 0 ? a : b;
            last = await host.PlayTurnAsync(state.MatchId, playerId, correct: playerId == a || turn % 3 == 0);
            host.Time.Advance(TimeSpan.FromSeconds(3));
            turn++;
            if (last.IsVictory)
            {
                break;
            }

            Assert.True(turn < 60, "The match should have ended.");
        }

        Assert.Equal(29, turn); // 15 turns for A, 14 for B
        Assert.Equal((MatchStatus.Finished, (int?)a, (int?)null), (last.State.Status, last.State.WinnerId, last.State.CurrentTurnPlayerId));
        Assert.Equal(15, last.NewPosition);

        // No more submissions or challenges once the match is over.
        var ex = await Assert.ThrowsAsync<GameRuleException>(() => host.GetChallengeAsync(state.MatchId, b));
        Assert.Equal(GameErrorCodes.MatchNotInProgress, ex.Code);
        ex = await Assert.ThrowsAsync<GameRuleException>(() => host.SubmitAsync(state.MatchId, last.PlayerChallengeId, AnswerFactory.Wrong));
        Assert.Equal(GameErrorCodes.MatchNotInProgress, ex.Code);

        // Results are persisted in the database (read with a fresh context).
        var stored = await host.QueryAsync(db => db.Matches.Include(m => m.Players).SingleAsync(m => m.Id == state.MatchId));
        Assert.Equal((MatchStatus.Finished, (int?)a), (stored.Status, stored.WinnerId));
        Assert.NotNull(stored.EndDate);
        var storedA = stored.Players.Single(p => p.PlayerId == a);
        Assert.Equal((15, 15, 15, 0), (storedA.CurrentPosition, storedA.Attempts, storedA.CorrectAnswers, storedA.WrongAnswers));
        Assert.Equal(0, await host.QueryAsync(db => db.PlayerChallenges.CountAsync(pc => pc.MatchId == state.MatchId && pc.Status == AttemptStatus.Pending)));

        var result = await host.RunAsync(g => g.GetResultAsync(state.MatchId));
        var winner = result.Players.Single(p => p.IsWinner);
        var loser = result.Players.Single(p => !p.IsWinner);
        Assert.Equal(("Le Caire", a), (result.DestinationName, winner.Player.Id));
        Assert.Equal(100, winner.AccuracyPercent);
        Assert.Equal(15, winner.ChallengesCompleted);
        Assert.Contains(winner.Badges, badge => badge.Code == "WORLD_EXPLORER");
        Assert.Contains(loser.Badges, badge => badge.Code == "BRAVE_LEARNER");
        Assert.Equal(14, loser.ChallengesAttempted);
        Assert.Equal(Math.Round(100.0 * loser.CorrectAnswers / 14, 1), loser.AccuracyPercent);
        Assert.Contains("Continue", loser.Message);
        Assert.True(result.TotalDuration > TimeSpan.Zero);
        Assert.True(winner.TimeSpentAnswering >= TimeSpan.Zero);
    }

    [Fact]
    public async Task Results_are_refused_while_the_match_is_in_progress()
    {
        await using var host = new GameTestHost(fixture);
        var state = await host.CreateMatchAsync();
        var ex = await Assert.ThrowsAsync<GameRuleException>(() => host.RunAsync(g => g.GetResultAsync(state.MatchId)));
        Assert.Equal(GameErrorCodes.MatchNotFinished, ex.Code);
    }
}
