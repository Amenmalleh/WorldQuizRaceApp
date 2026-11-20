using System.Text.Json;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Infrastructure.Seed;

/// <summary>Contenu de routes.json : une destination commune et des routes qui y mènent.</summary>
public sealed record RouteSeedFile(PlaceSeed Destination, IReadOnlyList<RouteSeed> Routes);

public sealed record RouteSeed(string Code, string Name, string? Description, IReadOnlyList<CheckpointSeed> Checkpoints);

/// <summary>Étape intermédiaire d'une route (la destination finale est ajoutée automatiquement en dernière position).</summary>
public sealed record CheckpointSeed(int Position, string Name, string Country, double Latitude, double Longitude, string FunFact);

public sealed record PlaceSeed(string Name, string Country, double Latitude, double Longitude, string FunFact);

/// <summary>Un défi de challenges.json. Content et CorrectAnswer sont conservés tels quels (JSON).</summary>
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
