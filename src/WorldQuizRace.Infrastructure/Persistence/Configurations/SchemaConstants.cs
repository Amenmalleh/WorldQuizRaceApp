namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

/// <summary>Constantes communes au schéma SQL.</summary>
internal static class SchemaConstants
{
    /// <summary>
    /// Position maximale autorisée par les contraintes CHECK (= nombre de checkpoints par route).
    /// Changer GameSettings:CheckpointsPerRoute impose d'adapter cette valeur, routes.json et une migration :
    /// le seeder refuse de démarrer si les deux divergent.
    /// </summary>
    public const int MaxRoutePosition = 15;

    /// <summary>Longueur des colonnes qui stockent un enum sous forme de texte.</summary>
    public const int EnumMaxLength = 30;
}
