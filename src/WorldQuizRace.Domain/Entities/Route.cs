namespace WorldQuizRace.Domain.Entities;

/// <summary>Predefined path of a player, from the starting point to the shared destination.</summary>
public class Route
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 300;

    public int Id { get; set; }

    /// <summary>Stable identifier in routes.json (e.g. "WEST"), used for idempotent synchronization.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int TotalCheckpoints { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Checkpoint> Checkpoints { get; set; } = [];
}
