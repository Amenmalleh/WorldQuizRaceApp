using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Map;
using WorldQuizRace.Infrastructure.Persistence;

namespace WorldQuizRace.Infrastructure.Seed;

/// <summary>
/// Synchronizes reference data (age groups, categories, badges, routes, challenges) with the database.
/// Idempotent: each item is matched on its natural key, then inserted or updated;
/// a second run without file changes writes nothing.
/// Challenges and routes missing from the files are deactivated, never deleted (matches may reference them).
/// </summary>
public sealed class DatabaseSeeder(
    WorldQuizRaceDbContext db,
    IOptions<GameSettings> gameSettings,
    ILogger<DatabaseSeeder> logger)
{
    // Keeps accents readable in the database; HTML-sensitive characters (<, >, &, ', ") stay escaped.
    private static readonly JsonSerializerOptions CompactJson = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <summary>Runs the synchronization and returns the number of rows written.</summary>
    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var routes = SeedDataReader.ReadRoutes();
        var challenges = SeedDataReader.ReadChallenges();
        ValidateRoutes(routes, gameSettings.Value.CheckpointsPerRoute);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var written = 0;
        written += await SyncAgeGroupsAsync(cancellationToken);
        written += await SyncCategoriesAsync(cancellationToken);
        written += await SyncBadgesAsync(cancellationToken);
        written += await SyncRoutesAsync(routes, cancellationToken);
        written += await SyncChallengesAsync(challenges, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Seed completed: {Rows} row(s) written ({Routes} routes, {Challenges} challenges in seed files).",
            written, routes.Routes.Count, challenges.Count);
        return written;
    }

    private async Task<int> SyncAgeGroupsAsync(CancellationToken ct)
    {
        var existing = await db.AgeGroups.ToDictionaryAsync(a => a.MinimumAge, ct);
        foreach (var seed in ReferenceData.AgeGroups)
        {
            if (!existing.TryGetValue(seed.MinimumAge, out var entity))
            {
                entity = new AgeGroup { MinimumAge = seed.MinimumAge };
                db.AgeGroups.Add(entity);
            }

            entity.Name = seed.Name;
            entity.MaximumAge = seed.MaximumAge;
        }

        return await db.SaveChangesAsync(ct);
    }

    private async Task<int> SyncCategoriesAsync(CancellationToken ct)
    {
        var existing = await db.Categories.ToDictionaryAsync(c => c.Name, ct);
        foreach (var seed in ReferenceData.Categories)
        {
            if (!existing.TryGetValue(seed.Name, out var entity))
            {
                entity = new Category { Name = seed.Name };
                db.Categories.Add(entity);
            }

            entity.Description = seed.Description;
        }

        return await db.SaveChangesAsync(ct);
    }

    private async Task<int> SyncBadgesAsync(CancellationToken ct)
    {
        var existing = await db.Badges.ToDictionaryAsync(b => b.Code, ct);
        foreach (var seed in ReferenceData.Badges)
        {
            if (!existing.TryGetValue(seed.Code, out var entity))
            {
                entity = new Badge { Code = seed.Code };
                db.Badges.Add(entity);
            }

            entity.Name = seed.Name;
            entity.Description = seed.Description;
            entity.Icon = seed.Icon;
        }

        // Badges removed from the reference data disappear, unless a player already earned them.
        var seededCodes = ReferenceData.Badges.Select(b => b.Code).ToHashSet(StringComparer.Ordinal);
        var obsolete = existing.Values.Where(b => !seededCodes.Contains(b.Code)).Select(b => b.Id).ToList();
        if (obsolete.Count > 0)
        {
            var awarded = await db.PlayerBadges.Where(pb => obsolete.Contains(pb.BadgeId)).Select(pb => pb.BadgeId).Distinct().ToListAsync(ct);
            db.Badges.RemoveRange(existing.Values.Where(b => obsolete.Contains(b.Id) && !awarded.Contains(b.Id)));
        }

        return await db.SaveChangesAsync(ct);
    }

    private async Task<int> SyncRoutesAsync(RouteSeedFile file, CancellationToken ct)
    {
        var existing = await db.Routes.Include(r => r.Checkpoints).ToDictionaryAsync(r => r.Code, ct);
        var seededCodes = file.Routes.Select(r => r.Code).ToHashSet(StringComparer.Ordinal);

        foreach (var seed in file.Routes)
        {
            if (!existing.TryGetValue(seed.Code, out var route))
            {
                route = new Route { Code = seed.Code };
                db.Routes.Add(route);
            }

            // The shared destination is appended as the last stop of every route: same name and coordinates guaranteed.
            var stops = seed.Checkpoints
                .Select(c => (c.Position, c.Name, c.Country, c.Latitude, c.Longitude, c.FunFact, IsFinal: false))
                .Append((Position: seed.Checkpoints.Count + 1, file.Destination.Name, file.Destination.Country,
                    file.Destination.Latitude, file.Destination.Longitude, file.Destination.FunFact, IsFinal: true))
                .ToList();

            route.Name = seed.Name;
            route.Description = seed.Description;
            route.TotalCheckpoints = stops.Count;
            route.IsActive = true;

            var byPosition = route.Checkpoints.ToDictionary(c => c.Position);
            foreach (var stop in stops)
            {
                if (!byPosition.Remove(stop.Position, out var checkpoint))
                {
                    checkpoint = new Checkpoint { Position = stop.Position };
                    route.Checkpoints.Add(checkpoint);
                }

                var (x, y) = MapProjection.ToMap(stop.Latitude, stop.Longitude);
                checkpoint.Name = stop.Name;
                checkpoint.Country = stop.Country;
                checkpoint.FunFact = stop.FunFact;
                checkpoint.MapX = x;
                checkpoint.MapY = y;
                checkpoint.IsFinal = stop.IsFinal;
            }

            // Extra stops (shortened route): no other table references checkpoints.
            db.Checkpoints.RemoveRange(byPosition.Values);
        }

        foreach (var orphan in existing.Values.Where(r => !seededCodes.Contains(r.Code)))
        {
            orphan.IsActive = false;
        }

        return await db.SaveChangesAsync(ct);
    }

    private async Task<int> SyncChallengesAsync(IReadOnlyList<ChallengeSeed> seeds, CancellationToken ct)
    {
        var ageGroups = await db.AgeGroups.ToDictionaryAsync(a => a.MinimumAge, a => a.Id, ct);
        var categories = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id, ct);
        var existing = await db.Challenges.ToDictionaryAsync(c => c.Code, ct);

        var duplicates = seeds.GroupBy(s => s.Code).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Duplicate challenge codes in seed: {string.Join(", ", duplicates)}.");
        }

        foreach (var seed in seeds)
        {
            if (!ageGroups.TryGetValue(seed.MinAge, out var ageGroupId))
            {
                throw new InvalidOperationException($"Challenge {seed.Code}: no age group starts at {seed.MinAge}.");
            }

            if (!categories.TryGetValue(seed.Category, out var categoryId))
            {
                throw new InvalidOperationException($"Challenge {seed.Code}: unknown category '{seed.Category}'.");
            }

            if (seed.Content.ValueKind != JsonValueKind.Object || seed.CorrectAnswer.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Challenge {seed.Code}: content and correctAnswer must be JSON objects.");
            }

            if (!existing.TryGetValue(seed.Code, out var challenge))
            {
                challenge = new Challenge { Code = seed.Code };
                db.Challenges.Add(challenge);
            }

            challenge.Title = seed.Title;
            challenge.Description = seed.Description;
            challenge.Type = seed.Type;
            challenge.AgeGroupId = ageGroupId;
            challenge.Difficulty = seed.Difficulty;
            challenge.CategoryId = categoryId;
            challenge.Content = JsonSerializer.Serialize(seed.Content, CompactJson);
            challenge.CorrectAnswer = JsonSerializer.Serialize(seed.CorrectAnswer, CompactJson);
            challenge.Explanation = seed.Explanation;
            challenge.TimeLimitSeconds = seed.TimeLimitSeconds;
            challenge.IsActive = true;
        }

        var seededCodes = seeds.Select(s => s.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var retired in existing.Values.Where(c => !seededCodes.Contains(c.Code)))
        {
            retired.IsActive = false;
        }

        return await db.SaveChangesAsync(ct);
    }

    /// <summary>Rejects inconsistent routes rather than creating an unfair race.</summary>
    internal static void ValidateRoutes(RouteSeedFile file, int checkpointsPerRoute)
    {
        if (file.Routes.Count < 2)
        {
            throw new InvalidOperationException("routes.json must define at least two routes.");
        }

        foreach (var route in file.Routes)
        {
            var total = route.Checkpoints.Count + 1; // + shared final destination
            if (total != checkpointsPerRoute)
            {
                throw new InvalidOperationException(
                    $"Route {route.Code} has {total} checkpoints (destination included) but GameSettings:CheckpointsPerRoute is {checkpointsPerRoute}.");
            }

            var positions = route.Checkpoints.Select(c => c.Position).ToList();
            if (!positions.SequenceEqual(Enumerable.Range(1, route.Checkpoints.Count)))
            {
                throw new InvalidOperationException($"Route {route.Code}: checkpoint positions must be 1..{route.Checkpoints.Count}, in order, without gaps.");
            }
        }
    }
}
