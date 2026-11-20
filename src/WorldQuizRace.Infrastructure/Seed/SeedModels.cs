using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Infrastructure.Seed;

/// <summary>Content of routes.json: a shared destination and the routes leading to it.</summary>
public sealed record RouteSeedFile(PlaceSeed Destination, IReadOnlyList<RouteSeed> Routes);

public sealed record RouteSeed(string Code, string Name, string? Description, IReadOnlyList<CheckpointSeed> Checkpoints);

/// <summary>Intermediate stop of a route (the final destination is appended automatically).</summary>
public sealed record CheckpointSeed(int Position, string Name, string Country, double Latitude, double Longitude, string FunFact);

public sealed record PlaceSeed(string Name, string Country, double Latitude, double Longitude, string FunFact);

/// <summary>A challenge from challenges.json. Content and CorrectAnswer are kept as raw JSON.</summary>
public sealed record ChallengeSeed(
    string Code,
    int MinAge,
    Difficulty Difficulty,
    string Category,
    ChallengeType Type,
    string Title,
    string? Description,
    JsonElement Content,
    JsonElement CorrectAnswer,
    string Explanation,
    int? TimeLimitSeconds);

public sealed record AgeGroupSeed(string Name, int MinimumAge, int MaximumAge);

public sealed record CategorySeed(string Name, string Description);

public sealed record BadgeSeed(string Code, string Name, string Description, string Icon);
