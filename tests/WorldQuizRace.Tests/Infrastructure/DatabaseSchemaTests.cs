using Microsoft.EntityFrameworkCore;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;
using WorldQuizRace.Infrastructure.Persistence;

namespace WorldQuizRace.Tests.Infrastructure;

/// <summary>Tests d'intégration sur une vraie base SQL Server (LocalDB) : migrations, seed et contraintes.</summary>
[Collection(LocalDbCollection.Name)]
public class DatabaseSchemaTests(LocalDbFixture fixture)
{
    [Fact]
    public async Task Seed_writes_data_once_then_is_idempotent()
    {
        Assert.True(fixture.FirstSeedRows > 0);

        await using var db = fixture.CreateContext();
        var secondRun = await fixture.CreateSeeder(db).SeedAsync();

        Assert.Equal(0, secondRun);
        Assert.Equal(4, await db.AgeGroups.CountAsync());
        Assert.Equal(10, await db.Categories.CountAsync());
        Assert.Equal(180, await db.Challenges.CountAsync(c => c.IsActive));
    }

    [Fact]
    public async Task Seeded_routes_have_equal_length_and_share_the_final_checkpoint()
    {
        await using var db = fixture.CreateContext();
        var routes = await db.Routes.Include(r => r.Checkpoints).Where(r => r.IsActive).ToListAsync();

        Assert.Equal(2, routes.Count);
        Assert.All(routes, r =>
        {
            Assert.Equal(15, r.TotalCheckpoints);
            Assert.Equal(Enumerable.Range(1, 15), r.Checkpoints.OrderBy(c => c.Position).Select(c => c.Position));
            Assert.Single(r.Checkpoints, c => c.IsFinal);
            Assert.True(r.Checkpoints.Single(c => c.IsFinal).Position == 15);
        });

        var finals = routes.Select(r => r.Checkpoints.Single(c => c.IsFinal)).ToList();
        Assert.Single(finals.Select(f => (f.Name, f.Country, f.MapX, f.MapY)).Distinct());
    }

    [Fact]
    public async Task Player_age_outside_5_to_15_is_rejected_by_the_database()
    {
        await using var db = fixture.CreateContext();
        var ageGroupId = await db.AgeGroups.Select(a => a.Id).FirstAsync();
        db.Players.Add(new Player { Name = "TropJeune", Age = 4, AgeGroupId = ageGroupId, CreatedAt = DateTimeOffset.UtcNow });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Only_one_pending_challenge_per_player_and_match_is_allowed()
    {
        await using var db = fixture.CreateContext();
        var (match, player, challengeIds) = await CreateMatchAsync(db);

        db.PlayerChallenges.Add(Pending(match.Id, player.Id, challengeIds[0]));
        await db.SaveChangesAsync();

        db.PlayerChallenges.Add(Pending(match.Id, player.Id, challengeIds[1]));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("UX_PlayerChallenges_OnePendingPerPlayer", ex.InnerException?.Message);
    }

    [Fact]
    public async Task A_new_pending_challenge_is_allowed_once_the_previous_one_is_answered()
    {
        await using var db = fixture.CreateContext();
        var (match, player, challengeIds) = await CreateMatchAsync(db);

        var first = Pending(match.Id, player.Id, challengeIds[0]);
        db.PlayerChallenges.Add(first);
        await db.SaveChangesAsync();

        first.Status = AttemptStatus.Answered;
        first.IsCorrect = true;
        first.Answer = """{"optionId":"b"}""";
        first.AnsweredAt = DateTimeOffset.UtcNow;
        db.PlayerChallenges.Add(Pending(match.Id, player.Id, challengeIds[1]));

        await db.SaveChangesAsync();
        Assert.Equal(2, await db.PlayerChallenges.CountAsync(pc => pc.MatchId == match.Id));
    }

    [Fact]
    public async Task Player_position_beyond_the_final_checkpoint_is_rejected()
    {
        await using var db = fixture.CreateContext();
        var (match, player, _) = await CreateMatchAsync(db);

        var participation = await db.MatchPlayers.SingleAsync(mp => mp.MatchId == match.Id && mp.PlayerId == player.Id);
        participation.CurrentPosition = 16;

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Concurrent_update_of_the_same_participation_is_detected()
    {
        var (matchId, playerId) = await CreateMatchIdsAsync();

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var a = await first.MatchPlayers.SingleAsync(mp => mp.MatchId == matchId && mp.PlayerId == playerId);
        var b = await second.MatchPlayers.SingleAsync(mp => mp.MatchId == matchId && mp.PlayerId == playerId);

        a.CurrentPosition = 1;
        await first.SaveChangesAsync();

        b.CurrentPosition = 1; // même tour soumis deux fois : la seconde écriture doit échouer
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_a_match_cascades_to_participations_and_attempts()
    {
        await using var db = fixture.CreateContext();
        var (match, player, challengeIds) = await CreateMatchAsync(db);
        db.PlayerChallenges.Add(Pending(match.Id, player.Id, challengeIds[0]));
        await db.SaveChangesAsync();

        db.Matches.Remove(match);
        await db.SaveChangesAsync();

        Assert.False(await db.MatchPlayers.AnyAsync(mp => mp.MatchId == match.Id));
        Assert.False(await db.PlayerChallenges.AnyAsync(pc => pc.MatchId == match.Id));
        Assert.True(await db.Players.AnyAsync(p => p.Id == player.Id));
    }

    private async Task<(int MatchId, int PlayerId)> CreateMatchIdsAsync()
    {
        await using var db = fixture.CreateContext();
        var (match, player, _) = await CreateMatchAsync(db);
        return (match.Id, player.Id);
    }

    private static async Task<(Match Match, Player PlayerA, List<int> ChallengeIds)> CreateMatchAsync(WorldQuizRaceDbContext db)
    {
        var ageGroup = await db.AgeGroups.SingleAsync(a => a.MinimumAge == 8);
        var routes = await db.Routes.OrderBy(r => r.Code).ToListAsync();
        var challengeIds = await db.Challenges.Where(c => c.AgeGroupId == ageGroup.Id).OrderBy(c => c.Id).Select(c => c.Id).Take(2).ToListAsync();

        var playerA = new Player { Name = "Adam", Age = 9, AgeGroup = ageGroup, CreatedAt = DateTimeOffset.UtcNow };
        var playerB = new Player { Name = "Lina", Age = 9, AgeGroup = ageGroup, CreatedAt = DateTimeOffset.UtcNow };
        var match = new Match { Status = MatchStatus.InProgress, StartDate = DateTimeOffset.UtcNow, TurnNumber = 1 };
        match.Players.Add(new MatchPlayer { Player = playerA, Slot = PlayerSlot.A, RouteId = routes[1].Id });
        match.Players.Add(new MatchPlayer { Player = playerB, Slot = PlayerSlot.B, RouteId = routes[0].Id });
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return (match, playerA, challengeIds);
    }

    private static PlayerChallenge Pending(int matchId, int playerId, int challengeId) => new()
    {
        MatchId = matchId,
        PlayerId = playerId,
        ChallengeId = challengeId,
        CheckpointPosition = 1,
        TurnNumber = 1,
        Status = AttemptStatus.Pending,
        ServedAt = DateTimeOffset.UtcNow
    };
}
