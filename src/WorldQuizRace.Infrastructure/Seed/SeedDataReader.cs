using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldQuizRace.Infrastructure.Seed;

/// <summary>
/// Reads the seed files embedded as resources in the Infrastructure assembly:
/// they ship with the DLL and therefore also work after publishing.
/// </summary>
public static class SeedDataReader
{
    public const string ChallengesResource = "WorldQuizRace.Seed.challenges.json";
    public const string RoutesResource = "WorldQuizRace.Seed.routes.json";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyList<ChallengeSeed> ReadChallenges() =>
        Read<List<ChallengeSeed>>(ChallengesResource);

    public static RouteSeedFile ReadRoutes() =>
        Read<RouteSeedFile>(RoutesResource);

    private static T Read<T>(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed resource '{resourceName}' was not found.");

        return JsonSerializer.Deserialize<T>(stream, Options)
            ?? throw new InvalidOperationException($"Seed resource '{resourceName}' is empty.");
    }
}
