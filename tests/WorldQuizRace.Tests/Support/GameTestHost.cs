using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using WorldQuizRace.Application;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Application.Services.Game;
using WorldQuizRace.Infrastructure;
using WorldQuizRace.Infrastructure.Persistence;
using WorldQuizRace.Tests.Infrastructure;

namespace WorldQuizRace.Tests.Support;

/// <summary>Random provider returning scripted values (then 0): makes challenge selection deterministic.</summary>
public sealed class ScriptedRandomProvider : IRandomProvider
{
    private readonly Queue<int> _values = new();

    public void Enqueue(params int[] values)
    {
        foreach (var value in values) _values.Enqueue(value);
    }

    public int Next(int maxExclusive) => _values.TryDequeue(out var value) ? value % maxExclusive : 0;
}

/// <summary>
/// The real Application + Infrastructure DI graph on the throwaway LocalDB database, with a fake clock
/// and scripted randomness. Every call runs in its own scope (a new DbContext), like separate HTTP requests.
/// </summary>
public sealed class GameTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public GameTestHost(LocalDbFixture fixture, Action<GameSettings>? configure = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{WorldQuizRace.Infrastructure.DependencyInjection.ConnectionStringName}"] = fixture.ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddSingleton<TimeProvider>(Time);
        services.AddSingleton<IRandomProvider>(Random);
        services.AddApplication();
        services.AddInfrastructure(configuration);
        if (configure is not null)
        {
            services.PostConfigure(configure);
        }

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));

    public ScriptedRandomProvider Random { get; } = new();

    public async Task<T> RunAsync<T>(Func<IGameService, Task<T>> action)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IGameService>());
    }

    public async Task<T> WithServiceAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        await using var scope = _provider.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task<T> QueryAsync<T>(Func<WorldQuizRaceDbContext, Task<T>> query)
    {
        await using var scope = _provider.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<WorldQuizRaceDbContext>());
    }

    // ---------- Game helpers ----------

    private static int _nameCounter;

    public Task<MatchStateDto> CreateMatchAsync(int ageA = 9, int ageB = 13) =>
        RunAsync(g => g.CreateMatchAsync(new CreateMatchRequest(
            new RegisterPlayerRequest($"Adam{Interlocked.Increment(ref _nameCounter)}", ageA, "fox"),
            new RegisterPlayerRequest($"Lina{Interlocked.Increment(ref _nameCounter)}", ageB, "panda"))));

    public Task<ChallengeDto> GetChallengeAsync(int matchId, int playerId) =>
        RunAsync(g => g.GetCurrentChallengeAsync(matchId, playerId));

    public Task<AnswerResultDto> SubmitAsync(int matchId, int attemptId, JsonElement answer) =>
        RunAsync(g => g.SubmitAnswerAsync(new SubmitAnswerRequest(matchId, attemptId, answer)));

    /// <summary>Correct answer of an attempt, read from the database (the test plays the role of a perfect player).</summary>
    public Task<JsonElement> CorrectAnswerAsync(int attemptId) =>
        QueryAsync(async db =>
        {
            var challenge = await db.PlayerChallenges.Where(pc => pc.Id == attemptId).Select(pc => pc.Challenge).SingleAsync();
            return AnswerFactory.Correct(challenge.Type, challenge.Content, challenge.CorrectAnswer);
        });

    /// <summary>Plays one full turn for the player whose turn it is.</summary>
    public async Task<AnswerResultDto> PlayTurnAsync(int matchId, int playerId, bool correct)
    {
        var challenge = await GetChallengeAsync(matchId, playerId);
        var answer = correct ? await CorrectAnswerAsync(challenge.PlayerChallengeId) : AnswerFactory.Wrong;
        return await SubmitAsync(matchId, challenge.PlayerChallengeId, answer);
    }

    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
