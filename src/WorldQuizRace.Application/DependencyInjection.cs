using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Answers;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Application.Services.Challenges;
using WorldQuizRace.Application.Services.Game;
using WorldQuizRace.Application.Services.Matches;
using WorldQuizRace.Application.Services.Players;
using WorldQuizRace.Application.Services.Progression;
using WorldQuizRace.Application.Services.Rewards;
using WorldQuizRace.Application.Services.Turns;

namespace WorldQuizRace.Application;

public static class DependencyInjection
{
    /// <summary>Registers the Application layer services and options.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<GameSettings>()
            .BindConfiguration(GameSettings.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<GameSettings>, GameSettingsValidator>();

        // Replaceable in tests (fake clock, deterministic randomness).
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IRandomProvider, SystemRandomProvider>();

        // Strategies discovered by reflection: adding a validator, badge rule or turn mode = adding a class.
        services.AddImplementationsOf<IAnswerValidator>();
        services.AddImplementationsOf<IBadgeRule>();
        services.AddImplementationsOf<ITurnStrategy>();

        services.AddSingleton<IAnswerValidationService, AnswerValidationService>();
        services.AddSingleton<IDifficultyPolicy, DifficultyPolicy>();
        services.AddSingleton<ITurnService, TurnService>();
        services.AddSingleton<IProgressionService, ProgressionService>();
        services.AddSingleton<IWinnerService, WinnerService>();

        services.AddScoped<IAgeGroupService, AgeGroupService>();
        services.AddScoped<IPlayerRegistrationService, PlayerRegistrationService>();
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IChallengeSelectionService, ChallengeSelectionService>();
        services.AddScoped<IRewardService, RewardService>();
        services.AddScoped<IMatchStateQuery, MatchStateQuery>();
        services.AddScoped<IMatchResultService, MatchResultService>();
        services.AddScoped<IGameService, GameService>();

        return services;
    }

    private static void AddImplementationsOf<TService>(this IServiceCollection services)
    {
        var implementations = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(TService).IsAssignableFrom(t));
        foreach (var implementation in implementations)
        {
            services.AddSingleton(typeof(TService), implementation);
        }
    }
}
