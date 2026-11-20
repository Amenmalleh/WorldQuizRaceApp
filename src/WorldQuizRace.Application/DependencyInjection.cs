using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Options;

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

        return services;
    }
}
