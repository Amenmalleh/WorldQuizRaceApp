using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorldQuizRace.Infrastructure.Persistence;
using WorldQuizRace.Tests.Infrastructure;
using WorldQuizRace.Tests.Support;

namespace WorldQuizRace.Tests.Web;

/// <summary>HTTP-level tests of /api/game: anti-forgery, status codes, ProblemDetails and JSON shape.</summary>
[Collection(LocalDbCollection.Name)]
public sealed class GameApiTests(LocalDbFixture fixture) : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("Database:InitializeOnStartup", "false");
        builder.ConfigureTestServices(services =>
            services.ConfigureDbContext<WorldQuizRaceDbContext>(options => options.UseSqlServer(fixture.ConnectionString)));
    });

    public void Dispose() => _factory.Dispose();

    private static object NewMatchBody(string suffix) => new
    {
        playerA = new { name = $"Adam {suffix}", age = 9, avatar = "fox" },
        playerB = new { name = $"Lina {suffix}", age = 13, avatar = "panda" }
    };

    private static async Task<HttpClient> ClientWithTokenAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var token = await client.GetFromJsonAsync<JsonObject>("/api/game/antiforgery");
        client.DefaultRequestHeaders.Add((string)token!["headerName"]!, (string)token["token"]!);
        return client;
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/game/matches", NewMatchBody("csrf"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_registration_returns_a_validation_problem()
    {
        var client = await ClientWithTokenAsync(_factory);
        var response = await client.PostAsJsonAsync("/api/game/matches", new
        {
            playerA = new { name = "A", age = 4 },
            playerB = new { name = "Lina", age = 16 }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("validation_failed", (string?)problem!["code"]);
        var errors = problem["errors"]!.AsObject().Select(e => e.Key).ToList();
        Assert.Contains("playerA.name", errors);
        Assert.Contains("playerA.age", errors);
        Assert.Contains("playerB.age", errors);
    }

    [Fact]
    public async Task Unknown_match_returns_404_problem()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/game/matches/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("match_not_found", (string?)(await response.Content.ReadFromJsonAsync<JsonObject>())!["code"]);
    }

    [Fact]
    public async Task A_turn_through_the_api_enforces_every_rule()
    {
        var client = await ClientWithTokenAsync(_factory);

        var created = await client.PostAsJsonAsync("/api/game/matches", NewMatchBody("api"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var state = await created.Content.ReadFromJsonAsync<JsonObject>();
        var matchId = (int)state!["matchId"]!;
        var players = state["players"]!.AsArray();
        var (a, b) = ((int)players[0]!["player"]!["id"]!, (int)players[1]!["player"]!["id"]!);
        Assert.Equal("InProgress", (string?)state["status"]);
        Assert.Equal("A", (string?)state["currentTurnSlot"]);

        // Player B tries to play during A's turn.
        var outOfTurn = await client.GetAsync($"/api/game/matches/{matchId}/players/{b}/challenge");
        Assert.Equal(HttpStatusCode.Conflict, outOfTurn.StatusCode);
        Assert.Equal("not_your_turn", (string?)(await outOfTurn.Content.ReadFromJsonAsync<JsonObject>())!["code"]);

        // Player A gets a challenge: public content only.
        var challengeResponse = await client.GetAsync($"/api/game/matches/{matchId}/players/{a}/challenge");
        var challengeJson = await challengeResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, challengeResponse.StatusCode);
        Assert.DoesNotContain("correctAnswer", challengeJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("explanation", challengeJson, StringComparison.OrdinalIgnoreCase);
        var attemptId = (int)JsonNode.Parse(challengeJson)!["playerChallengeId"]!;

        // Correct answer (the test reads it from the database), then the same request replayed.
        await using var host = new GameTestHost(fixture);
        var answer = await host.CorrectAnswerAsync(attemptId);
        var body = new { matchId, playerChallengeId = attemptId, answer };

        var first = await client.PostAsJsonAsync($"/api/game/matches/{matchId}/answers", body);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var result = await first.Content.ReadFromJsonAsync<JsonObject>();
        Assert.True((bool)result!["isCorrect"]!);
        Assert.Equal(1, (int)result["newPosition"]!);
        Assert.Equal(b, (int)result["state"]!["currentTurnPlayerId"]!);

        var replay = await client.PostAsJsonAsync($"/api/game/matches/{matchId}/answers", body);
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.Equal("attempt_already_answered", (string?)(await replay.Content.ReadFromJsonAsync<JsonObject>())!["code"]);

        var after = await client.GetFromJsonAsync<JsonObject>($"/api/game/matches/{matchId}");
        Assert.Equal(1, (int)after!["players"]![0]!["position"]!);

        // Mismatch between the URL and the body.
        var mismatch = await client.PostAsJsonAsync($"/api/game/matches/{matchId + 1}/answers", body);
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
    }

    [Fact]
    public async Task Results_of_an_unfinished_match_return_409()
    {
        var client = await ClientWithTokenAsync(_factory);
        var created = await client.PostAsJsonAsync("/api/game/matches", NewMatchBody("res"));
        var matchId = (int)(await created.Content.ReadFromJsonAsync<JsonObject>())!["matchId"]!;

        var response = await client.GetAsync($"/api/game/matches/{matchId}/result");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
