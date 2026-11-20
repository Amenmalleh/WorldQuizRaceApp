namespace WorldQuizRace.Infrastructure.Seed;

/// <summary>Short, stable reference data kept in code.</summary>
public static class ReferenceData
{
    public static IReadOnlyList<AgeGroupSeed> AgeGroups { get; } =
    [
        new("5–7 ans", 5, 7),
        new("8–10 ans", 8, 10),
        new("11–12 ans", 11, 12),
        new("13–15 ans", 13, 15)
    ];

    public static IReadOnlyList<CategorySeed> Categories { get; } =
    [
        new("Mathématiques", "Nombres, calcul et problèmes."),
        new("Logique", "Raisonnement, énigmes, suites et labyrinthes."),
        new("Géographie", "Pays, capitales, continents et paysages."),
        new("Sciences", "Le corps, la matière, l'espace et la nature."),
        new("Histoire", "Grandes périodes, événements et inventions."),
        new("Culture générale", "Le monde qui nous entoure au quotidien."),
        new("Formes et couleurs", "Reconnaître et trier formes et couleurs."),
        new("Animaux et nature", "Animaux, habitats et environnement."),
        new("Informatique", "Ordinateurs, binaire et algorithmes."),
        new("Mémoire", "Jeux de paires et de mémorisation.")
    ];

    public static IReadOnlyList<BadgeSeed> Badges { get; } =
    [
        new("FIRST_STEP", "Premier pas", "Réussir son tout premier défi.", "👣"),
        new("STREAK_3", "3 sans faute", "Réussir 3 défis d'affilée.", "🔥"),
        new("HALFWAY", "Mi-parcours", "Atteindre le milieu de sa route.", "🧭"),
        new("WORLD_EXPLORER", "Explorateur du monde", "Atteindre la destination finale.", "🌍"),
        new("BRAVE_LEARNER", "Apprenti courageux", "Terminer une partie, gagnée ou non.", "🌟")
    ];
}
