namespace WorldQuizRace.Domain.Entities;

/// <summary>Parcours prédéfini d'un joueur, de son point de départ jusqu'à la destination commune.</summary>
public class Route
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 300;

    public int Id { get; set; }

    /// <summary>Identifiant stable dans routes.json (ex. "WEST"), utilisé pour la synchronisation idempotente.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int TotalCheckpoints { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Checkpoint> Checkpoints { get; set; } = [];
}
