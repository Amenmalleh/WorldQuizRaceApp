namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

/// <summary>Constants shared by the SQL schema.</summary>
internal static class SchemaConstants
{
    /// <summary>
    /// Maximum position allowed by the CHECK constraints (= checkpoints per route).
    /// Changing GameSettings:CheckpointsPerRoute requires updating this value, routes.json and a migration:
    /// the seeder refuses to start when they disagree.
    /// </summary>
    public const int MaxRoutePosition = 15;

    /// <summary>Length of the columns that store an enum as text.</summary>
    public const int EnumMaxLength = 30;
}
