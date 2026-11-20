using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Options;

namespace WorldQuizRace.Application;

public static class DependencyInjection
{
    /// <summary>Enregistre les services et options de la couche Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<GameSettings>()
            .BindConfiguration(GameSettings.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<GameSettings>, GameSettingsValidator>();

        return services;
    }
}
